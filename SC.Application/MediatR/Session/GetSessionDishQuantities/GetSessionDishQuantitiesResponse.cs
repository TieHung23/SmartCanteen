namespace SC.Application.MediatR.Session.GetSessionDishQuantities;

public class GetSessionDishQuantitiesResponse
{
    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;

    /// <summary>Sum of <see cref="SessionDishQuantityDto.OrderedQuantity"/> across every dish.</summary>
    public int TotalOrderedQuantity { get; set; }

    /// <summary>
    /// The same dishes as <see cref="Dishes"/>, grouped into the categories the finalize
    /// budget is enforced against. Finalize is rejected unless every category's
    /// <see cref="SessionCategoryQuantityDto.PreparedQuantity"/> covers its
    /// <see cref="SessionCategoryQuantityDto.OrderedQuantity"/>.
    /// </summary>
    public List<SessionCategoryQuantityDto> Categories { get; set; } = new();

    /// <summary>Flat, ungrouped view of every dish. Kept for callers that predate <see cref="Categories"/>.</summary>
    public List<SessionDishQuantityDto> Dishes { get; set; } = new();
}

public class SessionCategoryQuantityDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Portions on order across every dish in this category.</summary>
    public int OrderedQuantity { get; set; }

    /// <summary>
    /// Portions the manager has committed to cooking across every dish in this category.
    /// Dishes with no prepared quantity recorded yet count as zero, so before the session is
    /// finalized this is normally 0 — the manager is still typing those numbers in.
    /// </summary>
    public int PreparedQuantity { get; set; }

    public List<SessionDishQuantityDto> Dishes { get; set; } = new();
}

public class SessionDishQuantityDto
{
    public Guid DishId { get; set; }
    public string DishName { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Portions of this dish currently on order for the session. Cancelled and expired
    /// orders, and refunded line items, are excluded — they no longer have to be served.
    /// </summary>
    public int OrderedQuantity { get; set; }

    /// <summary>
    /// Portions of this dish the manager has committed to cooking, or null while none has been
    /// recorded — either the session is not finalized yet, or the dish is no longer on its menu.
    /// </summary>
    public int? PreparedQuantity { get; set; }
}
