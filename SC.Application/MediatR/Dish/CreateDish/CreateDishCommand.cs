using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Dish.CreateDish;

public class CreateDishCommand : ICommand<CreateDishResponse>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public string? ImgUrl { get; set; }
}
