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
            var allNotifications = await notificationRepository
                .FindListAsync(notification =>
                    notification.RecipientId == userId
                    && !notification.IsDeleted,
                    cancellationToken);
            var filtered = allNotifications.AsEnumerable();

            if (request.IsRead.HasValue)
            {
                filtered = filtered.Where(notification =>
                    notification.IsRead == request.IsRead.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                var type = request.Type.Trim();
                filtered = filtered.Where(notification => notification.Type == type);
            }

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;
            var notifications = filteredList
                .OrderByDescending(notification => notification.CreatedAtUtc)
                .ThenByDescending(notification => notification.Id)
                .Skip(request.GetSkipCount())
                .Take(request.PageSize)
                .ToList();

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
