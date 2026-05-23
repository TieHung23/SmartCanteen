namespace SC.Infrastructure.DependencyInjection.Options;

public sealed class GoogleOptions
{
    public const string SectionName = "Google";

    /// <summary>
    /// The OAuth client IDs issued by Google Cloud Console. A Google ID token is accepted only
    /// when its <c>aud</c> claim matches one of these values. Provide every client your app
    /// signs in from — typically one Web client (frontend) plus one Android client per signing
    /// key (debug + release, or one per developer's debug key).
    /// </summary>
    public string[] ClientIds { get; set; } = Array.Empty<string>();
}
