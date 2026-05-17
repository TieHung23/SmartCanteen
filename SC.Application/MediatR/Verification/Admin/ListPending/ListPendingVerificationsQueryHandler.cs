using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Verification.AggregateRoot;
using SC.Domain.Domain.Verification.Enum;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Verification.Admin.ListPending;

internal class ListPendingVerificationsQueryHandler(
    IRepositoryBase<VerificationRequest, Guid> verificationRepository,
    IRepositoryBase<UserAggregate, Guid> userRepository,
    ILogger<ListPendingVerificationsQueryHandler> logger)
    : IQueryHandler<ListPendingVerificationsQuery, PaginatedList<PendingVerificationItem>>
{
    public async Task<Result<PaginatedList<PendingVerificationItem>>> Handle(
        ListPendingVerificationsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = verificationRepository
                .FindAll(v => v!.Status == VerificationStatus.Pending, cancellationToken);

            var total = await query.CountAsync(cancellationToken);

            var page = await query
                .OrderBy(v => v!.SubmittedAt)
                .Skip(request.GetSkipCount())
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var userIds = page.Select(v => v!.UserId).Distinct().ToList();
            var users = await userRepository
                .FindAll(u => userIds.Contains(u!.Id), cancellationToken)
                .ToDictionaryAsync(u => u!.Id, u => u, cancellationToken);

            var items = page.Select(v => new PendingVerificationItem(
                v!.Id,
                v.UserId,
                users.TryGetValue(v.UserId, out var u) ? u!.Email : string.Empty,
                users.TryGetValue(v.UserId, out var u2) ? u2!.Name : string.Empty,
                v.SubmittedAt,
                v.Documents.Count)).ToList();

            return Result.Success(
                new PaginatedList<PendingVerificationItem>(items, request.PageNumber, request.PageSize, total),
                "Pending verifications retrieved.");
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
