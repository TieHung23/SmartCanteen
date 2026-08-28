using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SC.Application.MediatR.Auth.Shared;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Auth;
using SC.Contract.Services.Email;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using UserAggregate = SC.Domain.Domain.User.User;
using PasswordResetTokenAggregate = SC.Domain.Domain.User.PasswordResetToken;

namespace SC.Application.MediatR.Auth.ForgotPassword;

internal class ForgotPasswordCommandHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<PasswordResetTokenAggregate, Guid> tokenRepository,
    IJwtTokenGenerator tokenGenerator,
    IEmailSender emailSender,
    IConfiguration configuration,
    IUnitOfWork unitOfWork,
    ILogger<ForgotPasswordCommandHandler> logger)
    : ICommandHandler<ForgotPasswordCommand, PasswordActionResponse>
{
    private const string ResponseMessage =
        "Nếu email này tồn tại tài khoản hợp lệ, liên kết đặt lại mật khẩu đã được gửi.";

    public async Task<Result<PasswordActionResponse>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await userRepository
                .FindSingleAsync(u => u.Email == normalizedEmail, cancellationToken);

            // Keep the same response for unknown and Google-only accounts to prevent enumeration.
            if (user is null || user.PasswordHash is null)
                return Success();

            var activeTokens = await tokenRepository
                .FindListAsync(t => t.UserId == user.Id && t.ConsumedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow, cancellationToken);

            var opaque = tokenGenerator.GenerateOpaqueToken();
            var ttl = TimeSpan.FromMinutes(configuration.GetValue("Jwt:PasswordResetMinutes", 60));
            var resetToken = PasswordResetTokenAggregate.Issue(user.Id, opaque.TokenHash, ttl);

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            foreach (var activeToken in activeTokens)
            {
                activeToken.Consume();
                tokenRepository.Update(activeToken);
            }

            await tokenRepository.AddAsync(resetToken, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            try
            {
                await emailSender.SendPasswordResetLinkAsync(
                    user.Email,
                    opaque.RawToken,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            }

            return Success();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error requesting password reset for {Email}", request.Email);
            return Result.Failure<PasswordActionResponse>(
                Error.ServerError,
                "Đã xảy ra lỗi khi yêu cầu đặt lại mật khẩu.");
        }
    }

    private static Result<PasswordActionResponse> Success()
    {
        return Result.Success(new PasswordActionResponse(ResponseMessage), ResponseMessage);
    }
}
