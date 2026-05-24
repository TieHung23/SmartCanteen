using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Order.DeleteOrder;

public class DeleteOrderCommand : ICommand<DeleteOrderResponse>
{
    public Guid Id { get; set; }

    public DeleteOrderCommand(Guid id)
    {
        Id = id;
    }
}
