namespace SC.Api.DependencyInjection.Options;

public sealed class NotificationRealtimeOptions
{
    public const string SectionName = "NotificationRealtime";

    public string HubPath { get; set; } = string.Empty;
    public string ClientEventName { get; set; } = string.Empty;
}
