namespace SC.Application.MediatR.Cart;

public sealed class CartData
{
    public Guid MealId { get; set; }
    public List<CartItemData>? Items { get; set; } = [];
}

public sealed class CartItemData
{
    public Guid DishId { get; set; }
    public int Quantity { get; set; }
}
