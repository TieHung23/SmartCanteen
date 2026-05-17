using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Verification.Admin.ListPending;

public class ListPendingVerificationsQuery : PaginationParams, IQuery<PaginatedList<PendingVerificationItem>>
{
    public ListPendingVerificationsQuery() { }

    public ListPendingVerificationsQuery(int pageNumber, int pageSize) : base(pageNumber, pageSize) { }
}

public record PendingVerificationItem(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserName,
    DateTimeOffset SubmittedAt,
    int DocumentCount);
