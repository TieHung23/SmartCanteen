namespace SC.Infrastructure.DependencyInjection.Options;

public sealed class LogUploadOptions
{
    public const string SectionName = "LogUpload";

    public bool Enabled { get; set; } = true;

    public string LocalLogsPath { get; set; } = "Logs";

    public TimeSpan UploadTimeUtc { get; set; } = new(7, 16, 0);
}
