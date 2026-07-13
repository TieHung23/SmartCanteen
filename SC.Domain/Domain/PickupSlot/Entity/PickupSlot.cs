using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.PickupSlot.Enum;

namespace SC.Domain.Domain.PickupSlot.Entity;

public class PickupSlot : Entity<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private PickupSlot() { }

    public string Code { get; private set; } = string.Empty;
    public PickupSlotStatus Status { get; private set; } = PickupSlotStatus.Empty;
    public Guid? OrderId { get; private set; }
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

    // Khay KHÔNG lưu trên ô: hộp kraft đã rời khay lúc staff lên kệ, khay về pool ngay.
    // "Đơn này dùng khay nào" tra ở ServingJob.TrayId (nguồn sự thật duy nhất).
    public void Assign(Guid orderId, Guid updatedBy) { OrderId = orderId; Status = PickupSlotStatus.Occupied; Touch(updatedBy); }
    public void Clear(Guid updatedBy) { OrderId = null; Status = PickupSlotStatus.Empty; Touch(updatedBy); }
    public void SoftDelete() { IsDeleted = true; DeletedAtUtc = DateTimeOffset.UtcNow; }
    private void Touch(Guid updatedBy) { UpdatedAtUtc = DateTimeOffset.UtcNow; UpdatedBy = updatedBy; }
}
