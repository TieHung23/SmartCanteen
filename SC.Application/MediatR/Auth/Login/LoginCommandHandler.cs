using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SC.Application.MediatR.Auth.Shared;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Auth;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.User.Enum;
using UserAggregate = SC.Domain.Domain.User.User;
using RefreshTokenAggregate = SC.Domain.Domain.User.RefreshToken;

namespace SC.Application.MediatR.Auth.Login;

internal class LoginCommandHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<RefreshTokenAggregate, Guid> refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator,
    IConfiguration configuration,
    IUnitOfWork unitOfWork,
    ILogger<LoginCommandHandler> logger) : ICommandHandler<LoginCommand, AuthTokensDto>
{
    public async Task<Result<AuthTokensDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await userRepository
                .FindSingleAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (user is null)
            {
                return Result.Failure<AuthTokensDto>(Error.InvalidCredentials, "Invalid email or password.");
            }

            // Accounts created via Google sign-in have no password hash — password
            // login is not available for them.
            if (user.PasswordHash is null)
            {
                return Result.Failure<AuthTokensDto>(
                    Error.InvalidCredentials,
                    "This account uses Google sign-in. Please continue with Google.");
            }

            if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Result.Failure<AuthTokensDto>(Error.InvalidCredentials, "Invalid email or password.");
            }

            if (!user.EmailVerified)
            {
                return Result.Failure<AuthTokensDto>(Error.EmailNotVerified, "Please verify your email before logging in.");
            }

            if (user.Status is AccountStatus.Suspended or AccountStatus.Banned)
            {
                return Result.Failure<AuthTokensDto>(Error.AccountSuspended, "This account has been suspended.");
            }

            var isFullyVerified = user.Status == AccountStatus.Active;

            var accessToken = tokenGenerator.GenerateAccessToken(
                user.Id,
                user.Email,
                user.Role.ToString(),
                isFullyVerified);

            var refreshOpaque = tokenGenerator.GenerateOpaqueToken();
            var refreshTtl = TimeSpan.FromDays(configuration.GetValue("Jwt:RefreshTokenDays", 7));
            var refreshToken = RefreshTokenAggregate.Issue(user.Id, refreshOpaque.TokenHash, refreshTtl);

            user.RecordLogin();

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(
                new AuthTokensDto(
                    accessToken.Token,
                    accessToken.ExpiresAt,
                    refreshOpaque.RawToken,
                    refreshToken.ExpiresAt),
                "Login successful.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error during login for {Email}", request.Email);
            return Result.Failure<AuthTokensDto>(Error.ServerError, "An error occurred while signing in.");
        }
    }
}
