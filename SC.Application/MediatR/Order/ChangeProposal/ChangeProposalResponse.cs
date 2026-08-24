namespace SC.Application.MediatR.Order.ChangeProposal;

public class ChangeProposalResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid CurrentDishId { get; set; }
    public string CurrentDishName { get; set; } = string.Empty;

    /// <summary>
    /// What the customer was actually charged for this order item - the exact amount a
    /// replacement dish has to cost for the swap to be accepted. Clients should filter the
    /// candidate dishes against this rather than against the current menu price of
    /// <see cref="CurrentDishId"/>: the two normally match, but a menu price edited after the
    /// order was placed would make the menu price the wrong number to compare with.
    /// Null once the item no longer carries the proposal's current dish (the proposal has
    /// already been swapped or refunded), where the value is no longer needed.
    /// </summary>
    public decimal? CurrentUnitPrice { get; set; }
    public Guid? SuggestedDishId { get; set; }
    public string? SuggestedDishName { get; set; }
    public Guid? SelectedDishId { get; set; }
    public string? SelectedDishName { get; set; }
    public bool IsRequiredItem { get; set; }
    public Guid? RequiredCategoryId { get; set; }
    public int ProposalStatus { get; set; }
    public List<string> AllowedActions { get; set; } = [];
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public bool IsExpired { get; set; }
    public DateTimeOffset? RespondedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
