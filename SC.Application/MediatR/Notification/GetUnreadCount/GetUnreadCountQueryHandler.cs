using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using NotificationAggregate = SC.Domain.Domain.Notification.AggregateRoot.Notification;

namespace SC.Application.MediatR.Notification.GetUnreadCount;

internal sealed class GetUnreadCountQueryHandler(
    IGenericRepository<NotificationAggregate, Guid> notificationRepository,
    ICurrentUserService currentUserService,
    ILogger<GetUnreadCountQueryHandler> logger)
    : IQueryHandler<GetUnreadCountQuery, GetUnreadCountResponse>
{
    public async Task<Result<GetUnreadCountResponse>> Handle(
        GetUnreadCountQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var notifications = await notificationRepository
                .FindListAsync(notification =>
                    notification.RecipientId == currentUserService.UserId
                    && !notification.IsRead
                    && !notification.IsDeleted,
                    cancellationToken);
            var count = notifications.Count;

            return Result.Success(
                new GetUnreadCountResponse(count),
                "Unread notification count retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve unread notification count");
            return Result.Failure<GetUnreadCountResponse>(
                Error.ServerError,
                "An error occurred while retrieving unread notification count.");
        }
    }
}
