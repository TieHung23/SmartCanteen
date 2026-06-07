namespace SC.Application.MediatR.Dish.UpdateDish;

public class UpdateDishResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public Guid CategoryId { get; set; }
    public string? ImgUrl { get; set; }
}
