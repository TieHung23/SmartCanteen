using System.Text.Json.Serialization;

namespace SC.Application.MediatR.Cart;

public sealed class CartData
{
    public List<CartSessionData> Sessions { get; set; } = [];
}

public sealed class CartSessionData
{
    public Guid SessionId { get; set; }
    public Guid MealTemplateId { get; set; }
    public List<CartItemData>? Items { get; set; } = [];
}

public sealed class CartItemData
{
    public Guid DishId { get; set; }
    public int Quantity { get; set; }

    [JsonIgnore]
    public string DishName { get; set; } = string.Empty;

    [JsonIgnore]
    public string? ImgUrl { get; set; }
}
