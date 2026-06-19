using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.ShelfStock.Entity;

/// <summary>
/// Tồn kho món trên kệ THEO PHIÊN (cùng món ở phiên khác nhau có số lượng độc lập).
/// Staff refill -&gt; tăng; robot gắp -&gt; giảm.
/// </summary>
public class ShelfStock : Entity<Guid>, IAuditableEntity<Guid>
{
    private ShelfStock()
    {
    }

    public required Guid SessionId { get; set; }   // phiên phục vụ
    public required Guid DishId { get; set; }
    public Guid? SlotConfigurationId { get; set; }
    public int Quantity { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static ShelfStock Create(Guid sessionId, Guid dishId, int quantity, Guid createdBy, Guid? slotConfigurationId = null)
    {
        return new ShelfStock
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            DishId = dishId,
            Quantity = quantity,
            SlotConfigurationId = slotConfigurationId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public bool TryDeduct(int amount, Guid updatedBy)
    {
        if (amount <= 0 || Quantity < amount)
            return false;
        Quantity -= amount;
        Touch(updatedBy);
        return true;
    }

    public void Refill(int amount, Guid updatedBy)
    {
        if (amount <= 0) return;
        Quantity += amount;
        Touch(updatedBy);
    }

    private void Touch(Guid updatedBy)
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
