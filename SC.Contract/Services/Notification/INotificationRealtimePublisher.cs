namespace SC.Contract.Services.Notification;

public interface INotificationRealtimePublisher
{
    Task PublishAsync(
        NotificationDelivery notification,
        CancellationToken cancellationToken = default);
}
