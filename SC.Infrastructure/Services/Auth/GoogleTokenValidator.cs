using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SC.Contract.Services.Auth;
using SC.Infrastructure.DependencyInjection.Options;

namespace SC.Infrastructure.Services.Auth;

/// <summary>
/// Verifies Google ID tokens using the official Google.Apis.Auth library. The library checks
/// the token signature against Google's published certificates, plus issuer and expiry; the
/// configured client IDs are enforced as accepted audiences (Web + Android clients).
/// </summary>
public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleOptions _options;
    private readonly ILogger<GoogleTokenValidator> _logger;

    public GoogleTokenValidator(IOptions<GoogleOptions> options, ILogger<GoogleTokenValidator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(idToken))
            return null;

        if (_options.ClientIds is null || _options.ClientIds.Length == 0)
        {
            throw new InvalidOperationException(
                "Google:ClientIds is not configured. Set at least one client ID in the application configuration.");
        }

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = _options.ClientIds
        };

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new GoogleUserInfo(
                Subject: payload.Subject,
                Email: payload.Email,
                EmailVerified: payload.EmailVerified,
                Name: string.IsNullOrWhiteSpace(payload.Name) ? payload.Email : payload.Name,
                PictureUrl: payload.Picture);
        }
        catch (InvalidJwtException ex)
        {
            // Token failed signature/issuer/audience/expiry validation — treat as unauthenticated.
            _logger.LogWarning(ex, "Google ID token validation failed.");
            return null;
        }
    }
}
