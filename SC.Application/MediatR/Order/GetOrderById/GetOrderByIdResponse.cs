namespace SC.Application.MediatR.Order.GetOrderById;

public class GetOrderByIdResponse
{
    public Guid Id { get; set; }
    public Guid MealId { get; set; }
    public Guid UserId { get; set; }
    public int Status { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "VND";
    public List<OrderItemDto> Items { get; set; } = new();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}

public class OrderItemDto
{
    public Guid DishId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "VND";
}
