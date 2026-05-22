namespace SC.Contract.Services.Auth;

/// <summary>
/// The verified identity carried by a Google ID token.
/// </summary>
public record GoogleUserInfo(
    string Subject,
    string Email,
    bool EmailVerified,
    string Name,
    string? PictureUrl);

/// <summary>
/// Verifies a Google ID token (signature, issuer, audience, expiry) issued by Google
/// Identity Services and returns the contained identity.
/// </summary>
public interface IGoogleTokenValidator
{
    /// <summary>
    /// Validates the given Google ID token. Returns <c>null</c> when the token is invalid,
    /// expired, or not issued for this application.
    /// </summary>
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
