namespace SC.Infrastructure.DependencyInjection.Options;

public sealed class NotificationTemplateOptions
{
    public const string SectionName = "NotificationTemplates";

    public Dictionary<string, NotificationTemplateDefinition> Templates { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class NotificationTemplateDefinition
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public string? ActionUrlTemplate { get; set; }
}
