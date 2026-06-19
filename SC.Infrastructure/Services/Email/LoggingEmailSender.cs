using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SC.Contract.Services.Email;
using SC.Infrastructure.DependencyInjection.Options;

namespace SC.Infrastructure.Services.Email;

/// <summary>
/// Fallback email sender used when Resend is not configured. Writes the
/// verification link to the application log so developers can copy/paste
/// it during local testing without needing a real provider.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly FrontendOptions _frontendOptions;
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(IOptions<FrontendOptions> frontendOptions, ILogger<LoggingEmailSender> logger)
    {
        _frontendOptions = frontendOptions.Value;
        _logger = logger;
    }

    public Task SendVerificationCodeAsync(
        string toEmail,
        string code,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "[Dev] No email provider configured. Verification code for {Email}: {Code}",
            toEmail,
            code);
        return Task.CompletedTask;
    }

    public Task SendVerificationStatusAsync(
        string toEmail,
        bool approved,
        string? rejectionReason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "[Dev] No email provider configured. Verification {Status} email for {Email}. Reason: {Reason}",
            approved ? "approved" : "rejected",
            toEmail,
            rejectionReason ?? "n/a");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(
        string toEmail,
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _frontendOptions.BaseUrl.TrimEnd('/');
        var path = _frontendOptions.ResetPasswordPath.StartsWith('/')
            ? _frontendOptions.ResetPasswordPath
            : "/" + _frontendOptions.ResetPasswordPath;
        var link = $"{baseUrl}{path}?token={Uri.EscapeDataString(rawToken)}";
        _logger.LogWarning(
            "[Dev] No email provider configured. Password reset link for {Email}: {Link}",
            toEmail,
            link);
        return Task.CompletedTask;
    }
}
