using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Auth;
using SC.Contract.Services.Email;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using UserAggregate = SC.Domain.Domain.User.User;
using EmailVerificationTokenAggregate = SC.Domain.Domain.User.EmailVerificationToken;

namespace SC.Application.MediatR.Auth.ResendVerificationCode;

internal class ResendVerificationCodeCommandHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<EmailVerificationTokenAggregate, Guid> tokenRepository,
    IJwtTokenGenerator tokenGenerator,
    IEmailSender emailSender,
    IConfiguration configuration,
    IUnitOfWork unitOfWork,
    ILogger<ResendVerificationCodeCommandHandler> logger)
    : ICommandHandler<ResendVerificationCodeCommand, ResendVerificationCodeResponse>
{
    public async Task<Result<ResendVerificationCodeResponse>> Handle(
        ResendVerificationCodeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await userRepository
                .FindSingleAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (user is null)
            {
                return Result.Failure<ResendVerificationCodeResponse>(
                    Error.UserNotFound,
                    "No account exists for this email.");
            }

            if (user.EmailVerified)
            {
                return Result.Failure<ResendVerificationCodeResponse>(
                    Error.EmailAlreadyVerified,
                    "This email is already verified. You can sign in now.");
            }

            if (!UserAggregate.RequiresEmailVerificationCode(user.Category))
            {
                return Result.Failure<ResendVerificationCodeResponse>(
                    Error.UnsupportedUserCategory,
                    "This account category does not use emailed verification codes.");
            }

            // A code is only reissued once the previous one has expired; an account that never
            // received a code (or whose codes are all consumed) passes straight through.
            var activeToken = await tokenRepository
                .FindSingleAsync(
                    t => t.UserId == user.Id
                         && t.ConsumedAt == null
                         && t.ExpiresAt > DateTimeOffset.UtcNow,
                    cancellationToken);

            if (activeToken is not null)
            {
                return Result.Failure<ResendVerificationCodeResponse>(
                    Error.VerificationCodeStillValid,
                    "A verification code is still valid. Please use it or wait until it expires.");
            }

            var verificationCode = tokenGenerator.GenerateEmailVerificationCode();
            var ttl = TimeSpan.FromMinutes(configuration.GetValue("Jwt:EmailVerificationCodeMinutes", 5));
            var verificationToken = EmailVerificationTokenAggregate.Issue(user.Id, verificationCode.CodeHash, ttl);

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await tokenRepository.AddAsync(verificationToken, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            try
            {
                await emailSender.SendVerificationCodeAsync(user.Email, verificationCode.Code, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to resend verification email to {Email}. The token is still persisted.",
                    user.Email);
            }

            var response = new ResendVerificationCodeResponse(user.Email);

            return Result.Success(response, "A new verification code has been sent to your email.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error resending verification code for {Email}", request.Email);
            return Result.Failure<ResendVerificationCodeResponse>(
                Error.ServerError,
                "An error occurred while resending the verification code.");
        }
    }
}
