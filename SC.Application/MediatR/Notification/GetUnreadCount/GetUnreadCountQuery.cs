using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Notification.GetUnreadCount;

public sealed record GetUnreadCountQuery : IQuery<GetUnreadCountResponse>;
