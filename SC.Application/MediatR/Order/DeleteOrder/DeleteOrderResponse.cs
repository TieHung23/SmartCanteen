namespace SC.Application.MediatR.Order.DeleteOrder;

public class DeleteOrderResponse
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
}
