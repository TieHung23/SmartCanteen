using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Notification.MarkAllNotificationsRead;

public sealed record MarkAllNotificationsReadCommand
    : ICommand<MarkAllNotificationsReadResponse>;
