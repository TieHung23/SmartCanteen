namespace SC.Application.MediatR.Order.GetAllOrders;

public class GetAllOrdersResponse
{
    public Guid Id { get; set; }
    public Guid MealId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid UserId { get; set; }
    public int Status { get; set; }
    public decimal TotalPrice { get; set; }
    public int ItemCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
