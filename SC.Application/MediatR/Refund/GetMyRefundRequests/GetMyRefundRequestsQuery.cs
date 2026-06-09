using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Refund.GetMyRefundRequests;

public sealed class GetMyRefundRequestsQuery
    : PaginationParams, IQuery<PaginatedList<GetMyRefundRequestsResponse>>
{
    public int? Status { get; set; }
}
