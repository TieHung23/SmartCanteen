using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using NotificationAggregate = SC.Domain.Domain.Notification.AggregateRoot.Notification;

namespace SC.Application.MediatR.Notification.DeleteNotification;

internal sealed class DeleteNotificationCommandHandler(
    IGenericRepository<NotificationAggregate, Guid> notificationRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<DeleteNotificationCommandHandler> logger)
    : ICommandHandler<DeleteNotificationCommand, DeleteNotificationResponse>
{
    public async Task<Result<DeleteNotificationResponse>> Handle(
        DeleteNotificationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var notification = await notificationRepository
                .FindSingleAsync(item =>
                    item.Id == request.Id
                    && item.RecipientId == currentUserService.UserId
                    && !item.IsDeleted,
                    cancellationToken);

            if (notification is null)
            {
                return Result.Failure<DeleteNotificationResponse>(
                    Error.NotificationNotFound,
                    "Notification was not found.");
            }

            notification.SoftDelete();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new DeleteNotificationResponse(notification.Id),
                "Notification deleted successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete notification {NotificationId}", request.Id);
            return Result.Failure<DeleteNotificationResponse>(
                Error.ServerError,
                "An error occurred while deleting the notification.");
        }
    }
}
