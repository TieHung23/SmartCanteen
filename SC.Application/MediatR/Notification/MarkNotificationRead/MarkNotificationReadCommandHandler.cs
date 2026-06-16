using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using NotificationAggregate = SC.Domain.Domain.Notification.AggregateRoot.Notification;

namespace SC.Application.MediatR.Notification.MarkNotificationRead;

internal sealed class MarkNotificationReadCommandHandler(
    IGenericRepository<NotificationAggregate, Guid> notificationRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<MarkNotificationReadCommandHandler> logger)
    : ICommandHandler<MarkNotificationReadCommand, NotificationResponse>
{
    public async Task<Result<NotificationResponse>> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            var notification = await notificationRepository
                .GetQueryable(item =>
                    item.Id == request.Id
                    && item.RecipientId == userId
                    && !item.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken);

            if (notification is null)
            {
                return Result.Failure<NotificationResponse>(
                    Error.NotificationNotFound,
                    "Notification was not found.");
            }

            notification.MarkAsRead(userId);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                notification.ToResponse(),
                "Notification marked as read.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark notification {NotificationId} as read", request.Id);
            return Result.Failure<NotificationResponse>(
                Error.ServerError,
                "An error occurred while updating the notification.");
        }
    }
}
