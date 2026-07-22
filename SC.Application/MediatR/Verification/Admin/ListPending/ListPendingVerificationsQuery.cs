using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Domain.Verification.Enum;

namespace SC.Application.MediatR.Verification.Admin.ListPending;

public class ListPendingVerificationsQuery : PaginationParams, IQuery<PaginatedList<PendingVerificationItem>>
{
    public ListPendingVerificationsQuery() { }

    public ListPendingVerificationsQuery(int pageNumber, int pageSize) : base(pageNumber, pageSize) { }

    public int? Status { get; set; }
}

public record PendingVerificationItem(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserName,
    VerificationStatus Status,
    DateTimeOffset SubmittedAt,
    int DocumentCount);
