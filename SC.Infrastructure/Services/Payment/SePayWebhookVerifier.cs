using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SC.Contract.Services.Payment;
using SC.Infrastructure.DependencyInjection.Options;

namespace SC.Infrastructure.Services.Payment;

public sealed class SePayWebhookVerifier(
    IOptions<SePayOptions> options,
    ILogger<SePayWebhookVerifier> logger) : ISePayWebhookVerifier
{
    private const string SignaturePrefix = "sha256=";
    private readonly SePayOptions _options = options.Value;

    public SePayWebhookVerificationResult Verify(
        string rawBody,
        string signature,
        string timestamp)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            logger.LogError("SePay webhook secret is not configured.");
            return new SePayWebhookVerificationResult(false, "Webhook authentication is not configured.");
        }

        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(timestamp))
        {
            return new SePayWebhookVerificationResult(false, "Missing SePay authentication headers.");
        }

        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var unixTimestamp))
        {
            return new SePayWebhookVerificationResult(false, "Invalid SePay timestamp.");
        }

        var toleranceSeconds = Math.Max(1, _options.WebhookTimestampToleranceSeconds);
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(currentTimestamp - unixTimestamp) > toleranceSeconds)
        {
            return new SePayWebhookVerificationResult(false, "SePay timestamp is outside the allowed window.");
        }

        if (!signature.StartsWith(SignaturePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new SePayWebhookVerificationResult(false, "Invalid SePay signature format.");
        }

        byte[] providedSignature;
        try
        {
            providedSignature = Convert.FromHexString(signature[SignaturePrefix.Length..]);
        }
        catch (FormatException)
        {
            return new SePayWebhookVerificationResult(false, "Invalid SePay signature format.");
        }

        var signedPayload = $"{timestamp}.{rawBody}";
        var expectedSignature = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_options.WebhookSecret),
            Encoding.UTF8.GetBytes(signedPayload));

        var isValid = providedSignature.Length == expectedSignature.Length
            && CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature);

        return isValid
            ? new SePayWebhookVerificationResult(true, "Webhook signature is valid.")
            : new SePayWebhookVerificationResult(false, "Invalid SePay signature.");
    }
}
