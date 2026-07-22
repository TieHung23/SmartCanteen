using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Tray.Enum;

namespace SC.Domain.Domain.Tray.Entity;

public class Tray : Entity<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private Tray() { }

    public string Code { get; private set; } = string.Empty;
    public TrayStatus Status { get; private set; } = TrayStatus.Available;
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

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

    // Khay chỉ giữ trạng thái bận/rảnh; "khay đang phục vụ đơn nào" tra ở ServingJob.TrayId.
    public void Reserve(Guid updatedBy) { Status = TrayStatus.Reserved; Touch(updatedBy); }
    public void MarkInUse(Guid updatedBy) { Status = TrayStatus.InUse; Touch(updatedBy); }
    public void Release(Guid updatedBy) { Status = TrayStatus.Available; Touch(updatedBy); }
    public void SoftDelete() { IsDeleted = true; DeletedAtUtc = DateTimeOffset.UtcNow; }
    private void Touch(Guid updatedBy) { UpdatedAtUtc = DateTimeOffset.UtcNow; UpdatedBy = updatedBy; }
}
