using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.ShelfStock.Entity;

public class ShelfStock : Entity<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private ShelfStock() { }

    public Guid SessionId { get; private set; }
    public Guid DishId { get; private set; }
    public Guid? SlotConfigurationId { get; private set; }
    public int Quantity { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static ShelfStock Create(Guid sessionId, Guid dishId, int quantity, Guid createdBy, Guid? slotConfigurationId = null)
    {
        return new ShelfStock { Id = Guid.NewGuid(), SessionId = sessionId, DishId = dishId, Quantity = quantity, SlotConfigurationId = slotConfigurationId, CreatedAtUtc = DateTimeOffset.UtcNow, CreatedBy = createdBy, UpdatedBy = createdBy };
    }

    public bool TryDeduct(int amount, Guid updatedBy) { if (amount <= 0 || Quantity < amount) return false; Quantity -= amount; Touch(updatedBy); return true; }
    public void Refill(int amount, Guid updatedBy) { if (amount <= 0) return; Quantity += amount; Touch(updatedBy); }
    public void SoftDelete() { IsDeleted = true; DeletedAtUtc = DateTimeOffset.UtcNow; }
    private void Touch(Guid updatedBy) { UpdatedAtUtc = DateTimeOffset.UtcNow; UpdatedBy = updatedBy; }
}
