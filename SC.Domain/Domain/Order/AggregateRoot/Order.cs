using SC.Domain.Abstraction.Entities;
using MealAggregate = SC.Domain.Domain.Meal.AggregateRoot.Meal;
using DishesAggregate = SC.Domain.Domain.Dishes.AggregateRoot.Dishes;
using PaymentAggregate = SC.Domain.Domain.Payment.AggregateRoot.Payment;
using SC.Domain.Domain.Order.ValueObject;

namespace SC.Domain.Domain.Order.AggregateRoot;

public class Order : Entity<Guid>, IAuditableEntity<Guid>
{
    public Guid MealId { get; set; }
    public MealAggregate? Meal { get; set; }

    public Guid? PaymentId { get; set; }
    public PaymentAggregate? Payment { get; set; }

    public IList<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    private Order() { }

    public static Order Create(MealAggregate meal, Guid createdBy)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            Meal = meal,
            MealId = meal.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void ChangeMeal(MealAggregate meal, Guid updatedBy)
    {
        Meal = meal;
        MealId = meal.Id;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void AddDishes(DishesAggregate dishes, int quantity)
    {
        OrderItems.Add(OrderItem.Create(dishes, quantity));
    }

    public void RemoveDishes(OrderItem orderItem)
    {
        OrderItems.Remove(orderItem);
    }
}