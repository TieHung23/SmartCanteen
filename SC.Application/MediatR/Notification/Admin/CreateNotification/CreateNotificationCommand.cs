using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Notification.Admin.CreateNotification;

public sealed class CreateNotificationCommand : ICommand<NotificationResponse>
{
    public Guid RecipientId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ActionUrl { get; set; }
    public Dictionary<string, object?>? Data { get; set; }
}
