using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Notification.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid Id)
    : ICommand<NotificationResponse>;
