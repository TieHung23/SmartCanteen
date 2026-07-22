using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Order.GetOrderById;

public class GetOrderByIdQuery : IQuery<GetOrderByIdResponse>
{
    public Guid Id { get; set; }
    public bool RequireOwner { get; set; } = true;

    public GetOrderByIdQuery(Guid id, bool requireOwner = true)
    {
        Id = id;
        RequireOwner = requireOwner;
    }
}
