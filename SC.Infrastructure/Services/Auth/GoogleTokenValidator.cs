using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SC.Contract.Services.Auth;
using SC.Infrastructure.DependencyInjection.Options;

namespace SC.Infrastructure.Services.Auth;

/// <summary>
/// Verifies Google ID tokens using the official Google.Apis.Auth library. The library checks
/// the token signature against Google's published certificates, plus issuer and expiry; the
/// configured client ID is enforced as the required audience.
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

        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new InvalidOperationException(
                "Google:ClientId is not configured. Set it in the application configuration.");
        }

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { _options.ClientId }
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
