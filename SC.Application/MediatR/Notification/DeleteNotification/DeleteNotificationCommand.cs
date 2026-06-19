using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Notification.DeleteNotification;

public sealed record DeleteNotificationCommand(Guid Id)
    : ICommand<DeleteNotificationResponse>;
