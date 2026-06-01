using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Dish.DeleteDish;

public record DeleteDishCommand(Guid Id) : ICommand<DeleteDishResponse>;
