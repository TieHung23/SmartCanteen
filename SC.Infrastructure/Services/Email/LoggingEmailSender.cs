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
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(IOptions<JwtOptions> jwtOptions, ILogger<LoggingEmailSender> logger)
    {
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public Task SendVerificationLinkAsync(
        string toEmail,
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        var link = $"{_jwtOptions.BaseUrl.TrimEnd('/')}/api/auth/verify-email?token={Uri.EscapeDataString(rawToken)}";
        _logger.LogWarning(
            "[Dev] No email provider configured. Verification link for {Email}: {Link}",
            toEmail,
            link);
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
}
