namespace SC.Application.MediatR.Dish.GetDishById;

public class GetDishByIdResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public List<Guid> MealIds { get; set; } = new();
    public Guid CategoryId { get; set; }
}
