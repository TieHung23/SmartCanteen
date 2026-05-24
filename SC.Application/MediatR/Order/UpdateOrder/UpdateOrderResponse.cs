namespace SC.Application.MediatR.Order.UpdateOrder;

public class UpdateOrderResponse
{
    public Guid Id { get; set; }
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;
}
