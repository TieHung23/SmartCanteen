using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Notification;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Notification.Admin.CreateNotification;

internal sealed class CreateNotificationCommandHandler(
    INotificationService notificationService,
    ILogger<CreateNotificationCommandHandler> logger)
    : ICommandHandler<CreateNotificationCommand, NotificationResponse>
{
    public async Task<Result<NotificationResponse>> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var delivery = await notificationService.SendAsync(
            new NotificationRequest(
                request.RecipientId,
                request.Type,
                request.Title,
                request.Message,
                request.ReferenceType,
                request.ReferenceId,
                request.ActionUrl,
                request.Data),
            cancellationToken);

        if (delivery is null)
        {
            logger.LogWarning(
                "Notification could not be created for recipient {RecipientId}",
                request.RecipientId);
            return Result.Failure<NotificationResponse>(
                Error.InvalidValue,
                "Notification could not be created. Verify the recipient and payload.");
        }

        return Result.Success(
            new NotificationResponse
            {
                Id = delivery.Id,
                Type = delivery.Type,
                Title = delivery.Title,
                Message = delivery.Message,
                ReferenceType = delivery.ReferenceType,
                ReferenceId = delivery.ReferenceId,
                ActionUrl = delivery.ActionUrl,
                DataJson = delivery.DataJson,
                IsRead = delivery.IsRead,
                CreatedAtUtc = delivery.CreatedAtUtc
            },
            "Notification created successfully.");
    }
}
