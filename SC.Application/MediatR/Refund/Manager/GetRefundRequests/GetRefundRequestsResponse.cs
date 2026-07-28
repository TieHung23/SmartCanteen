namespace SC.Application.MediatR.Refund.Manager.GetRefundRequests;

public sealed class GetRefundRequestsResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public int? OrderItemId { get; set; }
    public Guid? ChangeProposalId { get; set; }
    public Guid? DishId { get; set; }
    public string? DishName { get; set; }
    public Guid? CurrentDishId { get; set; }
    public string? CurrentDishName { get; set; }
    public Guid? SuggestedDishId { get; set; }
    public string? SuggestedDishName { get; set; }
    public Guid? SelectedDishId { get; set; }
    public string? SelectedDishName { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public decimal RefundPercent { get; set; }
    public decimal OrderAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ImageCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
}
