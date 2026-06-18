using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Tray.Enum;

namespace SC.Domain.Domain.Tray.Entity;

/// <summary>
/// Khay tái dùng trong pool. Mỗi khay có mã (ArUco/barcode) duy nhất để robot &amp; sensor nhận diện.
/// </summary>
public class Tray : Entity<Guid>, IAuditableEntity<Guid>
{
    private Tray()
    {
    }

    public required string Code { get; set; }
    public TrayStatus Status { get; set; } = TrayStatus.Available;
    public Guid? CurrentOrderId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static Tray Create(string code, Guid createdBy)
    {
        return new Tray
        {
            Id = Guid.NewGuid(),
            Code = code,
            Status = TrayStatus.Available,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void Reserve(Guid orderId, Guid updatedBy)
    {
        CurrentOrderId = orderId;
        Status = TrayStatus.Reserved;
        Touch(updatedBy);
    }

    public void MarkInUse(Guid updatedBy)
    {
        Status = TrayStatus.InUse;
        Touch(updatedBy);
    }

    public void MarkAtSlot(Guid updatedBy)
    {
        Status = TrayStatus.AtSlot;
        Touch(updatedBy);
    }

    public void Release(Guid updatedBy)
    {
        CurrentOrderId = null;
        Status = TrayStatus.Available;
        Touch(updatedBy);
    }

    private void Touch(Guid updatedBy)
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
