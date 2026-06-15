using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Order.CreateOrder;

public class CreateOrderCommand : ICommand<CreateOrderResponse>
{
    public long CartVersion { get; set; }
}
