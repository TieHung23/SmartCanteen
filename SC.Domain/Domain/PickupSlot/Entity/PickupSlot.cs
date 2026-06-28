using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.PickupSlot.Enum;

namespace SC.Domain.Domain.PickupSlot.Entity;

/// <summary>
/// Ô kệ pickup (HS quét QR tới lấy). KHÁC SlotConfiguration (lane robot gắp).
/// Khi HS lấy: dò ngược OrderId -&gt; Slot rồi set Empty; sensor ô là dự phòng.
/// </summary>
public class PickupSlot : Entity<Guid>, IAuditableEntity<Guid>
{
    private PickupSlot()
    {
    }

    public required string Code { get; set; }
    public PickupSlotStatus Status { get; set; } = PickupSlotStatus.Empty;
    public Guid? OrderId { get; set; }
    public Guid? TrayId { get; set; }

    /// <summary>Cảm biến ô báo có khay hay không (dự phòng khi dò ngược lỗi).</summary>
    public bool? SensorOccupied { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static PickupSlot Create(string code, Guid createdBy)
    {
        return new PickupSlot
        {
            Id = Guid.NewGuid(),
            Code = code,
            Status = PickupSlotStatus.Empty,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void Assign(Guid orderId, Guid trayId, Guid updatedBy)
    {
        OrderId = orderId;
        TrayId = trayId;
        Status = PickupSlotStatus.Occupied;
        SensorOccupied = true;
        Touch(updatedBy);
    }

    public void Clear(Guid updatedBy)
    {
        OrderId = null;
        TrayId = null;
        Status = PickupSlotStatus.Empty;
        SensorOccupied = false;
        Touch(updatedBy);
    }

    private void Touch(Guid updatedBy)
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
