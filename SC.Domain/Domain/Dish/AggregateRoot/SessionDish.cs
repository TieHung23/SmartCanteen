using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Domain.Domain.Dish.AggregateRoot;

public class SessionDish
{
    public Guid DishId { get; set; }
    public Guid SessionId { get; set; }
    public int Quantity { get; set; }

    public Dish Dish { get; set; } = null!;
    public SessionAggregateRoot Session { get; set; } = null!;
}
