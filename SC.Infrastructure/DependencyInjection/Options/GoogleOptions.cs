namespace SC.Infrastructure.DependencyInjection.Options;

public sealed class GoogleOptions
{
    public const string SectionName = "Google";

    /// <summary>
    /// The OAuth 2.0 Web client ID issued by Google Cloud Console. Google ID tokens are
    /// only accepted when their audience matches this value.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
}
