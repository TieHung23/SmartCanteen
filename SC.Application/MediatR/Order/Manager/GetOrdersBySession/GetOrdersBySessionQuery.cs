using SC.Application.MediatR.Order.GetAllOrders;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Order.Manager.GetOrdersBySession;

public sealed class GetOrdersBySessionQuery : PaginationParams, IQuery<PaginatedList<GetAllOrdersResponse>>
{
    public Guid SessionId { get; set; }
    public int? Status { get; set; }

    public GetOrdersBySessionQuery()
    {
    }

    public GetOrdersBySessionQuery(
        Guid sessionId,
        int pageNumber = 1,
        int pageSize = 10,
        int? status = null)
        : base(pageNumber, pageSize)
    {
        SessionId = sessionId;
        Status = status;
    }
}
