using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Order.GetAllOrders;

public class GetAllOrdersQuery : PaginationParams, IQuery<PaginatedList<GetAllOrdersResponse>>
{
    public Guid? UserId { get; set; }
    public Guid? SessionId { get; set; }
    public int? Status { get; set; }
    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedTo { get; set; }
    public DateTimeOffset? SessionDateFrom { get; set; }
    public DateTimeOffset? SessionDateTo { get; set; }

    public GetAllOrdersQuery()
    {
    }

    public GetAllOrdersQuery(
        int pageNumber = 1,
        int pageSize = 10,
        Guid? userId = null,
        Guid? sessionId = null,
        int? status = null,
        DateTimeOffset? createdFrom = null,
        DateTimeOffset? createdTo = null,
        DateTimeOffset? sessionDateFrom = null,
        DateTimeOffset? sessionDateTo = null)
        : base(pageNumber, pageSize)
    {
        UserId = userId;
        SessionId = sessionId;
        Status = status;
        CreatedFrom = createdFrom;
        CreatedTo = createdTo;
        SessionDateFrom = sessionDateFrom;
        SessionDateTo = sessionDateTo;
    }
}
