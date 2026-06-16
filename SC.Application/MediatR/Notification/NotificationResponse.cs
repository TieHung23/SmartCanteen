namespace SC.Application.MediatR.Notification;

public sealed class NotificationResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ActionUrl { get; init; }
    public string? DataJson { get; init; }
    public bool IsRead { get; init; }
    public DateTimeOffset? ReadAtUtc { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
