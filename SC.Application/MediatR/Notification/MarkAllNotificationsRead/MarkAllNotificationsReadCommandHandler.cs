using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using NotificationAggregate = SC.Domain.Domain.Notification.AggregateRoot.Notification;

namespace SC.Application.MediatR.Notification.MarkAllNotificationsRead;

internal sealed class MarkAllNotificationsReadCommandHandler(
    IGenericRepository<NotificationAggregate, Guid> notificationRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<MarkAllNotificationsReadCommandHandler> logger)
    : ICommandHandler<MarkAllNotificationsReadCommand, MarkAllNotificationsReadResponse>
{
    public async Task<Result<MarkAllNotificationsReadResponse>> Handle(
        MarkAllNotificationsReadCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            var notifications = await notificationRepository
                .FindListAsync(notification =>
                    notification.RecipientId == userId
                    && !notification.IsRead
                    && !notification.IsDeleted,
                    cancellationToken);

            foreach (var notification in notifications)
            {
                notification.MarkAsRead(userId);
            }

            if (notifications.Count > 0)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result.Success(
                new MarkAllNotificationsReadResponse(notifications.Count),
                "Notifications marked as read.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark all notifications as read");
            return Result.Failure<MarkAllNotificationsReadResponse>(
                Error.ServerError,
                "An error occurred while updating notifications.");
        }
    }
}
