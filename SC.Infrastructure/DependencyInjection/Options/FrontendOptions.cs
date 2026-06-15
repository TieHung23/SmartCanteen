namespace SC.Infrastructure.DependencyInjection.Options;

/// <summary>
/// Where the SmartCanteen frontend is served. Used to build user-facing links
/// (email verification, password reset, etc.) that point at the FE so the user
/// lands on a styled page rather than a raw backend JSON response.
/// </summary>
public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>
    /// Base URL of the frontend app, no trailing slash (e.g. <c>http://178.128.100.1:3000</c>).
    /// Local developers should override this in <c>appsettings.Development.json</c> /
    /// <c>appsettings.Local.json</c> with their local FE origin (typically
    /// <c>http://localhost:3000</c>).
    /// </summary>
    public string BaseUrl { get; set; } = "http://178.128.100.1:3000";

    /// <summary>
    /// Path the FE owns for handling the email verification token. The backend
    /// appends <c>?token=...</c> to this path when building the verification link.
    /// </summary>
    public string VerifyEmailPath { get; set; } = "/verify-email";

    public string ResetPasswordPath { get; set; } = "/reset-password";
}
