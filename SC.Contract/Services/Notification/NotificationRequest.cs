namespace SC.Contract.Services.Notification;

public sealed record NotificationRequest(
    Guid RecipientId,
    string Type,
    string Title,
    string Message,
    string? ReferenceType = null,
    Guid? ReferenceId = null,
    string? ActionUrl = null,
    object? Data = null);
