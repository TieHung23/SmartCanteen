using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Order.GetOrderById;

public class GetOrderByIdQuery : IQuery<GetOrderByIdResponse>
{
    public Guid Id { get; set; }

    public GetOrderByIdQuery(Guid id)
    {
        Id = id;
    }
}
