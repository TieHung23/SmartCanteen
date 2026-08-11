namespace SC.Application.MediatR.Session.GetSessionDishQuantities;

public class GetSessionDishQuantitiesResponse
{
    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;

    /// <summary>Sum of <see cref="SessionDishQuantityDto.OrderedQuantity"/> across every dish.</summary>
    public int TotalOrderedQuantity { get; set; }

    public List<SessionDishQuantityDto> Dishes { get; set; } = new();
}

public class SessionDishQuantityDto
{
    public Guid DishId { get; set; }
    public string DishName { get; set; } = string.Empty;

    /// <summary>
    /// Portions of this dish currently on order for the session. Cancelled and expired
    /// orders, and refunded line items, are excluded — they no longer have to be served.
    /// </summary>
    public int OrderedQuantity { get; set; }
}
