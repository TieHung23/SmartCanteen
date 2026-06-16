using SC.Contract.Services.Notification;
using NotificationAggregate = SC.Domain.Domain.Notification.AggregateRoot.Notification;

namespace SC.Application.MediatR.Notification;

internal static class NotificationMapping
{
    public static NotificationResponse ToResponse(this NotificationAggregate notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            ReferenceType = notification.ReferenceType,
            ReferenceId = notification.ReferenceId,
            ActionUrl = notification.ActionUrl,
            DataJson = notification.DataJson,
            IsRead = notification.IsRead,
            ReadAtUtc = notification.ReadAtUtc,
            CreatedAtUtc = notification.CreatedAtUtc
        };
    }

    public static NotificationDelivery ToDelivery(this NotificationAggregate notification)
    {
        return new NotificationDelivery(
            notification.Id,
            notification.RecipientId,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.ReferenceType,
            notification.ReferenceId,
            notification.ActionUrl,
            notification.DataJson,
            notification.IsRead,
            notification.CreatedAtUtc);
    }
}
