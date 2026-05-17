using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Dish.DeleteDish;

public class DeleteDishCommand : ICommand
{
    public Guid Id { get; set; }

    public DeleteDishCommand()
    {
    }

    public DeleteDishCommand(Guid id)
    {
        Id = id;
    }
}
