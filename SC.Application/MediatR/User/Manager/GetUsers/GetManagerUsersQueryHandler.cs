using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.User.Enum;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.User.Manager.GetUsers;

internal class GetManagerUsersQueryHandler(
    IGenericRepository<UserAggregate, Guid> userRepository,
    ILogger<GetManagerUsersQueryHandler> logger)
    : IQueryHandler<GetManagerUsersQuery, PaginatedList<ManagerUserListItem>>
{
    public async Task<Result<PaginatedList<ManagerUserListItem>>> Handle(
        GetManagerUsersQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var users = await userRepository.FindListAsync(
                user => !user.IsDeleted,
                cancellationToken);

            var filtered = users.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                filtered = filtered.Where(user =>
                    user.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || user.Email.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(user.StudentId)
                        && user.StudentId.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            if (request.Status.HasValue)
                filtered = filtered.Where(user => user.Status == request.Status.Value);

            if (request.Role.HasValue)
                filtered = filtered.Where(user => user.Role == request.Role.Value);

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;
            var page = filteredList
                .OrderByDescending(user => user.CreatedAtUtc)
                .Skip(request.GetSkipCount())
                .Take(request.PageSize)
                .Select(user => new ManagerUserListItem(
                    user.Id,
                    user.Name,
                    user.Email,
                    user.ImgUrl,
                    user.Role,
                    user.Status,
                    GetCurrentStatusReason(user.Status, user.StatusReason),
                    user.EmailVerified,
                    user.StudentId,
                    user.MajorOrClass,
                    user.PhoneNumber,
                    user.Balance.Amount,
                    user.LastLoginAt,
                    user.CreatedAtUtc))
                .ToList();

            return Result.Success(
                new PaginatedList<ManagerUserListItem>(
                    page,
                    request.PageNumber,
                    request.PageSize,
                    totalCount),
                "Users retrieved.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving users for manager");
            return Result.Failure<PaginatedList<ManagerUserListItem>>(
                Error.ServerError,
                "An error occurred while retrieving users.");
        }
    }

    private static string? GetCurrentStatusReason(AccountStatus status, string? statusReason)
    {
        return status is AccountStatus.Suspended or AccountStatus.Banned
            ? statusReason
            : null;
    }
}
