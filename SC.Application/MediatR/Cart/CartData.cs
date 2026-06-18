namespace SC.Application.MediatR.Cart;

public sealed class CartData
{
    public List<CartMealData> Meals { get; set; } = [];
}

public sealed class CartMealData
{
    public Guid MealId { get; set; }
    public Guid MealTemplateId { get; set; }
    public List<CartItemData>? Items { get; set; } = [];
}

public sealed class CartItemData
{
    public Guid DishId { get; set; }
    public int Quantity { get; set; }
}
