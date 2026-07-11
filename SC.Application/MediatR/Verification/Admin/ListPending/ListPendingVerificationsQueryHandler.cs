using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Verification.AggregateRoot;
using SC.Domain.Domain.Verification.Enum;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Verification.Admin.ListPending;

internal class ListPendingVerificationsQueryHandler(
    IGenericRepository<VerificationRequest, Guid> verificationRepository,
    IGenericRepository<UserAggregate, Guid> userRepository,
    ILogger<ListPendingVerificationsQueryHandler> logger)
    : IQueryHandler<ListPendingVerificationsQuery, PaginatedList<PendingVerificationItem>>
{
    public async Task<Result<PaginatedList<PendingVerificationItem>>> Handle(
        ListPendingVerificationsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var status = request.Status ?? (int)VerificationStatus.Pending;
            if (!Enum.IsDefined(typeof(VerificationStatus), status))
            {
                return Result.Failure<PaginatedList<PendingVerificationItem>>(
                    Error.InvalidValue,
                    "Verification status is invalid.");
            }

            var allPending = await verificationRepository
                .FindListAsync(v => (int)v.Status == status, cancellationToken);

            var total = allPending.Count;
            var page = allPending
                .OrderBy(v => v.SubmittedAt)
                .Skip(request.GetSkipCount())
                .Take(request.PageSize)
                .ToList();

            var userIds = page.Select(v => v!.UserId).Distinct().ToList();
            var usersList = await userRepository
                .FindListAsync(u => userIds.Contains(u.Id), cancellationToken);
            var users = usersList.ToDictionary(u => u.Id, u => u);

            var items = page.Select(v => new PendingVerificationItem(
                v!.Id,
                v.UserId,
                users.TryGetValue(v.UserId, out var u) ? u!.Email : string.Empty,
                users.TryGetValue(v.UserId, out var u2) ? u2!.Name : string.Empty,
                v.Status,
                v.SubmittedAt,
                v.Documents.Count)).ToList();

            return Result.Success(
                new PaginatedList<PendingVerificationItem>(items, request.PageNumber, request.PageSize, total),
                "Verifications retrieved.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing pending verifications");
            return Result.Failure<PaginatedList<PendingVerificationItem>>(
                Error.ServerError,
                "An error occurred while retrieving pending verifications.");
        }
    }
}
