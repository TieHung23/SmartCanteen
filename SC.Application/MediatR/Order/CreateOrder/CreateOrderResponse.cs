namespace SC.Application.MediatR.Order.CreateOrder;

public class CreateOrderResponse
{
    public Guid Id { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "VND";
    public string Message { get; set; } = string.Empty;
    public decimal UserRemainingBalance { get; set; }
}
