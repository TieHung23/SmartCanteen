using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Session.FinalizeSession;

public class FinalizeSessionCommand : ICommand<FinalizeSessionResponse>
{
    public Guid SessionId { get; set; }
    public List<PreparedDishDto> PreparedDishes { get; set; } = [];
}

public class PreparedDishDto
{
    public Guid DishId { get; set; }
    public int PreparedQuantity { get; set; }
}
