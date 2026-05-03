namespace SC.Infrastructure.DependencyInjection.Options;

public sealed class CloundinaryOptions
{
    public const string SectionName = "Cloudinary";

    public string CloudName { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;

    public string ImagesFolder { get; set; } = "images";

    public string LogsFolder { get; set; } = "logs";
}
