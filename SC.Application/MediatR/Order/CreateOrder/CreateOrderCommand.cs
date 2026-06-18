using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Order.CreateOrder;

public class CreateOrderCommand : ICommand<CreateOrderResponse>
{
    public Guid SessionId { get; set; }
    public long CartVersion { get; set; }
}
