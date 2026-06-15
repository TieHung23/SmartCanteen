using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Order.ValueObject;
using SC.Domain.Domain.Order.Enum;
using MealAggregate = SC.Domain.Domain.Meal.AggregateRoot.Meal;
using MealTemplateEntity = SC.Domain.Domain.Meal.Entity.MealTemplate;

namespace SC.Domain.Domain.Order.AggregateRoot;

public class Order : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private Order()
    {
    }

    public Guid MealId { get; set; }
    public MealAggregate Meal { get; set; } = null!;
    public Guid? MealTemplateId { get; set; }
    public MealTemplateEntity? MealTemplate { get; set; }

    public Guid? WalletTransactionId { get; set; }

    public IList<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static Order Create(Guid mealId, Guid mealTemplateId, Guid createdBy)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            MealId = mealId,
            MealTemplateId = mealTemplateId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            Status = OrderStatus.Pending
        };
    }

    public void ChangeMeal(Guid mealId, Guid updatedBy)
    {
        MealId = mealId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void AddDish(Guid dishId, int quantity, decimal unitPriceAmount)
    {
        OrderItems.Add(OrderItem.Create(dishId, quantity, unitPriceAmount));
    }

    public void AttachTransaction(Guid walletTransactionId, Guid updatedBy)
    {
        WalletTransactionId = walletTransactionId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void RemoveDish(OrderItem orderItem)
    {
        OrderItems.Remove(orderItem);
    }

    public void UpdateStatus(OrderStatus status, Guid updatedBy)
    {
        Status = status;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
