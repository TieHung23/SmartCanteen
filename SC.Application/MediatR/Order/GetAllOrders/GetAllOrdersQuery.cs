using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Order.GetAllOrders;

public class GetAllOrdersQuery : PaginationParams, IQuery<PaginatedList<GetAllOrdersResponse>>
{
    public Guid? UserId { get; set; }
    public Guid? SessionId { get; set; }
    public int? Status { get; set; }

    public GetAllOrdersQuery()
    {
    }

    public GetAllOrdersQuery(
        int pageNumber = 1,
        int pageSize = 10,
        Guid? userId = null,
        Guid? sessionId = null,
        int? status = null)
        : base(pageNumber, pageSize)
    {
        UserId = userId;
        SessionId = sessionId;
        Status = status;
    }
}
