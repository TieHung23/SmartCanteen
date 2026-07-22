namespace SC.Application.MediatR.Order.ChangeProposal;

public class ChangeProposalResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid CurrentDishId { get; set; }
    public string CurrentDishName { get; set; } = string.Empty;
    public Guid? SuggestedDishId { get; set; }
    public string? SuggestedDishName { get; set; }
    public Guid? SelectedDishId { get; set; }
    public string? SelectedDishName { get; set; }
    public bool IsRequiredItem { get; set; }
    public Guid? RequiredCategoryId { get; set; }
    public int ProposalStatus { get; set; }
    public List<string> AllowedActions { get; set; } = [];
    public DateTimeOffset? RespondedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
