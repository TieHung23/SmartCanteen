using System.Text.Json;
using Microsoft.Extensions.Logging;
using SC.Application.MediatR.Notification;
using SC.Contract.Services.Notification;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using NotificationAggregate = SC.Domain.Domain.Notification.AggregateRoot.Notification;

namespace SC.Application.Services;

internal sealed class NotificationService(
    IGenericRepository<NotificationAggregate, Guid> notificationRepository,
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    INotificationRealtimePublisher realtimePublisher,
    INotificationPushPublisher pushPublisher,
    ILogger<NotificationService> logger) : INotificationService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<NotificationDelivery?> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var recipient = await userRepository.GetByIdAsync(
                request.RecipientId,
                cancellationToken);

            if (recipient is null || recipient.IsDeleted)
            {
                logger.LogWarning(
                    "Notification recipient {RecipientId} was not found",
                    request.RecipientId);
                return null;
            }

            var dataJson = request.Data is null
                ? null
                : JsonSerializer.Serialize(request.Data, JsonOptions);

            var notification = NotificationAggregate.Create(
                request.RecipientId,
                request.Type,
                request.Title,
                request.Message,
                request.ReferenceType,
                request.ReferenceId,
                request.ActionUrl,
                dataJson,
                currentUserService.UserId);

            await notificationRepository.AddAsync(notification, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var delivery = notification.ToDelivery();

            try
            {
                await realtimePublisher.PublishAsync(delivery, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Realtime delivery failed for notification {NotificationId}; it remains available through the API",
                    notification.Id);
            }

            try
            {
                await pushPublisher.PublishAsync(delivery, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Native push delivery failed for notification {NotificationId}; it remains available through the API",
                    notification.Id);
            }

            return delivery;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to create notification for recipient {RecipientId}",
                request.RecipientId);
            return null;
        }
    }
}
