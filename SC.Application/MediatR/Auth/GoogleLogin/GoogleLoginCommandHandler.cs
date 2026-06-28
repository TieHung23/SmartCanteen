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

namespace SC.Application.MediatR.Auth.GoogleLogin;

internal class GoogleLoginCommandHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<RefreshTokenAggregate, Guid> refreshTokenRepository,
    IGoogleTokenValidator googleTokenValidator,
    IJwtTokenGenerator tokenGenerator,
    IConfiguration configuration,
    IUnitOfWork unitOfWork,
    ILogger<GoogleLoginCommandHandler> logger) : ICommandHandler<GoogleLoginCommand, AuthTokensDto>
{
    public async Task<Result<AuthTokensDto>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var googleUser = await googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken);
            if (googleUser is null)
            {
                return Result.Failure<AuthTokensDto>(
                    Error.GoogleTokenInvalid, "Google sign-in token is invalid or expired.");
            }

            if (!googleUser.EmailVerified)
            {
                return Result.Failure<AuthTokensDto>(
                    Error.GoogleEmailNotVerified, "The Google account email address is not verified.");
            }

            var email = googleUser.Email.Trim().ToLowerInvariant();

            // BR-01/BR-03: Google sign-in is the fast path for trusted FPT accounts only.
            if (!UserAggregate.IsFptEmail(email))
            {
                return Result.Failure<AuthTokensDto>(
                    Error.NonFptGoogleAccount,
                    "Google sign-in is only available for FPT University accounts.");
            }

            var user = await userRepository
                .FindSingleAsync(u => u.Email == email, cancellationToken);

            var isNewUser = user is null;

            if (isNewUser)
            {
                user = await CreateUserFromGoogleAsync(googleUser, email, cancellationToken);
            }
            else
            {
                if (user!.Status is AccountStatus.Suspended or AccountStatus.Banned)
                {
                    return Result.Failure<AuthTokensDto>(
                        Error.AccountSuspended, "This account has been suspended.");
                }

                // First Google sign-in for an account originally registered with a password.
                if (user.GoogleSubjectId is null)
                {
                    user.LinkGoogle(googleUser.Subject);
                }
            }

            user!.RecordLogin();

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            if (isNewUser)
                await userRepository.AddAsync(user, cancellationToken);
            else
                userRepository.Update(user);

            var refreshOpaque = tokenGenerator.GenerateOpaqueToken();
            var refreshTtl = TimeSpan.FromDays(configuration.GetValue("Jwt:RefreshTokenDays", 7));
            var refreshToken = RefreshTokenAggregate.Issue(user.Id, refreshOpaque.TokenHash, refreshTtl);

            await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var accessToken = tokenGenerator.GenerateAccessToken(
                user.Id,
                user.Email,
                user.Role.ToString(),
                user.Status == AccountStatus.Active);

            return Result.Success(
                new AuthTokensDto(
                    accessToken.Token,
                    accessToken.ExpiresAt,
                    refreshOpaque.RawToken,
                    refreshToken.ExpiresAt),
                "Google sign-in successful.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error during Google sign-in");
            return Result.Failure<AuthTokensDto>(
                Error.ServerError, "An error occurred while signing in with Google.");
        }
    }

    private async Task<UserAggregate> CreateUserFromGoogleAsync(
        GoogleUserInfo googleUser,
        string email,
        CancellationToken cancellationToken)
    {
        var hasStudentCode = UserAggregate.TryExtractStudentId(email, out var studentId);

        // BR-07: an institutional ID maps to a single account. If the code parsed from the
        // email is already taken, store none rather than blocking the sign-in.
        if (studentId is not null)
        {
            var studentIdTaken = await userRepository
                .ExistsAsync(u => u.StudentId == studentId, cancellationToken);

            if (studentIdTaken)
            {
                logger.LogWarning(
                    "Google sign-in for {Email}: parsed student id {StudentId} is already in use; storing none.",
                    email, studentId);
                studentId = null;
            }
        }

        return UserAggregate.RegisterWithGoogle(
            name: googleUser.Name,
            email: email,
            googleSubjectId: googleUser.Subject,
            studentId: studentId,
            imgUrl: googleUser.PictureUrl);
    }
}
