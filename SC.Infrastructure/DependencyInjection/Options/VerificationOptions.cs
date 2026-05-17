namespace SC.Infrastructure.DependencyInjection.Options;

public sealed class VerificationOptions
{
    public const string SectionName = "Verification";

    public long FileSizeMaxBytes { get; set; } = 5 * 1024 * 1024;

    public string[] AcceptedFormats { get; set; } =
        new[] { "image/jpeg", "image/png", "application/pdf" };

    public int EmailTokenHours { get; set; } = 24;

    public int RequestExpiryDays { get; set; } = 14;
}
