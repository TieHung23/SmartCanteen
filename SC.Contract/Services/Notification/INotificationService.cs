namespace SC.Contract.Services.Notification;

public interface INotificationService
{
    Task<NotificationDelivery?> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default);
}
