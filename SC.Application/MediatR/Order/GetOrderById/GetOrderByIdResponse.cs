namespace SC.Application.MediatR.Order.GetOrderById;

public class GetOrderByIdResponse
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid? MealTemplateId { get; set; }
    public Guid? TransactionId { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImgUrl { get; set; }
    public int Status { get; set; }
    public decimal TotalPrice { get; set; }
    // Mã khay ĐANG gắn với job phục vụ của đơn (staff dựa vào để lấy đúng khay).
    // null khi: đơn chưa vào phục vụ, chưa bind khay, HOẶC đã lên kệ (khay đã trả về pool).
    public string? TrayCode { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public List<OrderStatusHistoryDto> StatusHistories { get; set; } = new();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}

public class OrderItemDto
{
    public Guid DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public string? ImgUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int ItemStatus { get; set; }
}

public class OrderStatusHistoryDto
{
    public Guid Id { get; set; }
    public int? FromStatus { get; set; }
    public int ToStatus { get; set; }
    public string? ReasonCode { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
}
