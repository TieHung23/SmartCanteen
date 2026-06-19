namespace SC.Contract.Services.Notification;

public sealed record NotificationDelivery(
    Guid Id,
    Guid RecipientId,
    string Type,
    string Title,
    string Message,
    string? ReferenceType,
    Guid? ReferenceId,
    string? ActionUrl,
    string? DataJson,
    bool IsRead,
    DateTimeOffset CreatedAtUtc);
