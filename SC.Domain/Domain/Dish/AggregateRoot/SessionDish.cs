using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.Dish;

public class SessionDish : Entity<Guid>
{
    private SessionDish() { }

    public Guid DishId { get; private set; }
    public Guid SessionId { get; private set; }
    public int? PreparedQuantity { get; private set; }

    public static SessionDish Create(Guid dishId, Guid sessionId)
    {
        return new SessionDish
        {
            Id = Guid.NewGuid(),
            DishId = dishId,
            SessionId = sessionId
        };
    }

    public void SetPreparedQuantity(int preparedQuantity)
    {
        if (preparedQuantity < 0)
            throw new ArgumentException("Prepared quantity cannot be negative.", nameof(preparedQuantity));
        PreparedQuantity = preparedQuantity;
    }

    public bool HasEnoughFor(int orderedQuantity) => PreparedQuantity.HasValue && PreparedQuantity >= orderedQuantity;
}
