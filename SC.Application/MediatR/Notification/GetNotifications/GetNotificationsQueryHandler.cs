using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using NotificationAggregate = SC.Domain.Domain.Notification.AggregateRoot.Notification;

namespace SC.Application.MediatR.Notification.GetNotifications;

internal sealed class GetNotificationsQueryHandler(
    IGenericRepository<NotificationAggregate, Guid> notificationRepository,
    ICurrentUserService currentUserService,
    ILogger<GetNotificationsQueryHandler> logger)
    : IQueryHandler<GetNotificationsQuery, PaginatedList<NotificationResponse>>
{
    public async Task<Result<PaginatedList<NotificationResponse>>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            var query = notificationRepository
                .GetQueryable(notification =>
                    notification.RecipientId == userId
                    && !notification.IsDeleted)
                .AsNoTracking();

            if (request.IsRead.HasValue)
            {
                query = query.Where(notification =>
                    notification.IsRead == request.IsRead.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                var type = request.Type.Trim();
                query = query.Where(notification => notification.Type == type);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var notifications = await query
                .OrderByDescending(notification => notification.CreatedAtUtc)
                .ThenByDescending(notification => notification.Id)
                .Skip(request.GetSkipCount())
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return Result.Success(
                new PaginatedList<NotificationResponse>(
                    notifications.Select(notification => notification.ToResponse()).ToList(),
                    request.PageNumber,
                    request.PageSize,
                    totalCount),
                "Notifications retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve notifications for the current user");
            return Result.Failure<PaginatedList<NotificationResponse>>(
                Error.ServerError,
                "An error occurred while retrieving notifications.");
        }
    }
}
