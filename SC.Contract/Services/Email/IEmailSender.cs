namespace SC.Contract.Services.Email;

public interface IEmailSender
{
    Task SendVerificationLinkAsync(
        string toEmail,
        string rawToken,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetLinkAsync(
        string toEmail,
        string rawToken,
        CancellationToken cancellationToken = default);

    Task SendVerificationStatusAsync(
        string toEmail,
        bool approved,
        string? rejectionReason,
        CancellationToken cancellationToken = default);
}
