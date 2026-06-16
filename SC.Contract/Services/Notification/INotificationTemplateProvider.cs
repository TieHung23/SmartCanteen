namespace SC.Contract.Services.Notification;

public interface INotificationTemplateProvider
{
    NotificationTemplate? Render(
        string key,
        IReadOnlyDictionary<string, string> tokens);
}

public sealed record NotificationTemplate(
    string Type,
    string Title,
    string MessageTemplate,
    string? ReferenceType,
    string? ActionUrlTemplate);
