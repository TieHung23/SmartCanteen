using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Notification.GetNotifications;

public sealed class GetNotificationsQuery
    : PaginationParams, IQuery<PaginatedList<NotificationResponse>>
{
    public bool? IsRead { get; set; }
    public string? Type { get; set; }
}
