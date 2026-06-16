using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using SC.Api.DependencyInjection.Options;
using SC.Api.Hubs;
using SC.Contract.Services.Notification;

namespace SC.Api.Services;

public sealed class SignalRNotificationRealtimePublisher(
    IHubContext<NotificationHub> hubContext,
    IOptions<NotificationRealtimeOptions> options)
    : INotificationRealtimePublisher
{
    private readonly NotificationRealtimeOptions _options = options.Value;

    public Task PublishAsync(
        NotificationDelivery notification,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientEventName))
        {
            throw new InvalidOperationException(
                "NotificationRealtime:ClientEventName is not configured.");
        }

        return hubContext.Clients
            .User(notification.RecipientId.ToString())
            .SendAsync(
                _options.ClientEventName,
                notification,
                cancellationToken);
    }
}
