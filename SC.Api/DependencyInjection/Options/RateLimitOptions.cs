namespace SC.Api.DependencyInjection.Options;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int PermitLimit { get; set; } = 100;
    public int Window { get; set; } = 60; // In seconds
    public int QueueLimit { get; set; } = 0;
}
