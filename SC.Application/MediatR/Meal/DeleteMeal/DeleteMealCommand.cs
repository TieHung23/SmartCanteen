using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Meal.DeleteMeal;

public class DeleteMealCommand : ICommand<DeleteMealResponse>
{
    public Guid Id { get; set; }

    public DeleteMealCommand(Guid id)
    {
        Id = id;
    }
}
