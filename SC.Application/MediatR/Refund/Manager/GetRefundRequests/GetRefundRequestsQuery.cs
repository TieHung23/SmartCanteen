using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Refund.Manager.GetRefundRequests;

public sealed class GetRefundRequestsQuery
    : PaginationParams, IQuery<PaginatedList<GetRefundRequestsResponse>>
{
    public int? Status { get; set; }
}
