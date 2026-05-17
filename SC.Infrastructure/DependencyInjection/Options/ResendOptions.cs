namespace SC.Infrastructure.DependencyInjection.Options;

public sealed class ResendOptions
{
    public const string SectionName = "Resend";

    public string ApiKey { get; set; } = string.Empty;

    public string FromEmail { get; set; } = "onboarding@resend.dev";

    public string FromName { get; set; } = "SmartCanteen";

    public string ApiBaseUrl { get; set; } = "https://api.resend.com";
}
