using SC.Contract.Abstraction.Message;
using SC.Domain.Domain.Order.Enum;

namespace SC.Application.MediatR.Order.UpdateOrder;

public class UpdateOrderCommand : ICommand<UpdateOrderResponse>
{
    public Guid Id { get; set; }
    public int Status { get; set; }
}
