using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SC.Contract.Services.Email;
using SC.Infrastructure.DependencyInjection.Options;

namespace SC.Infrastructure.Services.Email;

public sealed class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly ResendOptions _resendOptions;
    private readonly FrontendOptions _frontendOptions;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        HttpClient httpClient,
        IOptions<ResendOptions> resendOptions,
        IOptions<FrontendOptions> frontendOptions,
        ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _resendOptions = resendOptions.Value;
        _frontendOptions = frontendOptions.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_resendOptions.ApiBaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _resendOptions.ApiKey);
    }

    public Task SendVerificationLinkAsync(
        string toEmail,
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _frontendOptions.BaseUrl.TrimEnd('/');
        var path = _frontendOptions.VerifyEmailPath.StartsWith('/')
            ? _frontendOptions.VerifyEmailPath
            : "/" + _frontendOptions.VerifyEmailPath;
        var link = $"{baseUrl}{path}?token={Uri.EscapeDataString(rawToken)}";
        var html = $@"
            <p>Welcome to SmartCanteen!</p>
            <p>Click the link below to verify your email address:</p>
            <p><a href=""{link}"">Verify my email</a></p>
            <p>If you did not create an account, you can ignore this email.</p>";

        return SendAsync(toEmail, "Verify your SmartCanteen email", html, cancellationToken);
    }

    public Task SendVerificationStatusAsync(
        string toEmail,
        bool approved,
        string? rejectionReason,
        CancellationToken cancellationToken = default)
    {
        string subject;
        string html;
        if (approved)
        {
            subject = "Your SmartCanteen account is now active";
            html = "<p>Your identity verification was approved. You can now place orders and top up your wallet.</p>";
        }
        else
        {
            subject = "Your SmartCanteen verification was rejected";
            html = $@"
                <p>Your identity verification was rejected.</p>
                <p>Reason: <em>{System.Net.WebUtility.HtmlEncode(rejectionReason ?? "Not specified")}</em></p>
                <p>Please submit a new verification request with corrected information.</p>";
        }

        return SendAsync(toEmail, subject, html, cancellationToken);
    }

    private async Task SendAsync(string toEmail, string subject, string html, CancellationToken cancellationToken)
    {
        var payload = new
        {
            from = $"{_resendOptions.FromName} <{_resendOptions.FromEmail}>",
            to = new[] { toEmail },
            subject,
            html
        };

        using var response = await _httpClient.PostAsJsonAsync("/emails", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Resend API call failed: {StatusCode} {Body}",
                response.StatusCode,
                body);
            throw new InvalidOperationException(
                $"Failed to send email via Resend (status {(int)response.StatusCode}).");
        }
    }
}
