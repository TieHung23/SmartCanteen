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
                return Result.Failure<AuthTokensDto>(Error.InvalidCredentials, "Email hoặc mật khẩu không đúng.");
            }

            // Accounts created via Google sign-in have no password hash — password
            // login is not available for them.
            if (user.PasswordHash is null)
            {
                return Result.Failure<AuthTokensDto>(
                    Error.InvalidCredentials,
                    "Tài khoản này đăng nhập bằng Google. Vui lòng tiếp tục với Google.");
            }

            if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Result.Failure<AuthTokensDto>(Error.InvalidCredentials, "Email hoặc mật khẩu không đúng.");
            }

            if (!user.EmailVerified)
            {
                return Result.Failure<AuthTokensDto>(Error.EmailNotVerified, "Vui lòng xác minh email trước khi đăng nhập.");
            }

            if (user.Status == AccountStatus.Banned)
            {
                return Result.Failure<AuthTokensDto>(
                    Error.AccountBanned,
                    "Tài khoản này đã bị cấm.",
                    user.StatusReason);
            }

            if (user.Status == AccountStatus.Suspended)
            {
                return Result.Failure<AuthTokensDto>(
                    Error.AccountSuspended,
                    "Tài khoản này đã bị tạm khóa.",
                    user.StatusReason);
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
                "Đăng nhập thành công.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error during login for {Email}", request.Email);
            return Result.Failure<AuthTokensDto>(Error.ServerError, "Đã xảy ra lỗi khi đăng nhập.");
        }
    }
}
