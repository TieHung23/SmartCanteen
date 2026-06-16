namespace SC.Contract.Services.Notification;

public interface INotificationPushPublisher
{
    Task PublishAsync(
        NotificationDelivery notification,
        CancellationToken cancellationToken = default);
}
