using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.PickupSlot.Enum;

namespace SC.Domain.Domain.PickupSlot.Entity;

public class PickupSlot : Entity<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private PickupSlot() { }

    public string Code { get; private set; } = string.Empty;
    public PickupSlotStatus Status { get; private set; } = PickupSlotStatus.Empty;
    public Guid? OrderId { get; private set; }
    public Guid? TrayId { get; private set; }
    public bool? SensorOccupied { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static PickupSlot Create(string code, Guid createdBy)
    {
        return new PickupSlot { Id = Guid.NewGuid(), Code = code, Status = PickupSlotStatus.Empty, CreatedAtUtc = DateTimeOffset.UtcNow, CreatedBy = createdBy, UpdatedBy = createdBy };
    }

    public void Assign(Guid orderId, Guid trayId, Guid updatedBy) { OrderId = orderId; TrayId = trayId; Status = PickupSlotStatus.Occupied; SensorOccupied = true; Touch(updatedBy); }
    public void MarkWaitingCollect(Guid updatedBy) { Status = PickupSlotStatus.WaitingCollect; Touch(updatedBy); }
    public void Clear(Guid updatedBy) { OrderId = null; TrayId = null; Status = PickupSlotStatus.Empty; SensorOccupied = false; Touch(updatedBy); }
    public void SoftDelete() { IsDeleted = true; DeletedAtUtc = DateTimeOffset.UtcNow; }
    private void Touch(Guid updatedBy) { UpdatedAtUtc = DateTimeOffset.UtcNow; UpdatedBy = updatedBy; }
}
