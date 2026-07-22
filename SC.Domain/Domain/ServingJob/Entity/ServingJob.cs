using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.ServingJob.Enum;

namespace SC.Domain.Domain.ServingJob.Entity;

public class ServingJob : Entity<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private ServingJob() { }

    public Guid OrderId { get; private set; }
    public Guid? TrayId { get; private set; }
    public Guid? PickupSlotId { get; private set; }
    public ServingJobStatus Status { get; private set; } = ServingJobStatus.Queued;
    public DateTimeOffset? PushedAtUtc { get; private set; }
    public DateTimeOffset? AcknowledgedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static ServingJob Create(Guid orderId, Guid createdBy, Guid? trayId = null)
    {
        return new ServingJob
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            TrayId = trayId,
            Status = ServingJobStatus.Queued,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void AssignTray(Guid trayId, Guid updatedBy)
    {
        TrayId = trayId;
        Touch(updatedBy);
    }

    // Mô hình dây chuyền: 1 job nhiều tay cùng làm -> job KHÔNG giữ RobotArmId.
    // "Tay nào gắp món nào" ghi ở RobotEventLog (mức món, resolve từ station robot báo).
    public void MarkPushed(Guid updatedBy)
    {
        Status = ServingJobStatus.Pushed;
        PushedAtUtc = DateTimeOffset.UtcNow;
        Touch(updatedBy);
    }

    public void Acknowledge(Guid updatedBy)
    {
        Status = ServingJobStatus.Assembling;
        AcknowledgedAtUtc = DateTimeOffset.UtcNow;
        Touch(updatedBy);
    }

    public void MarkOnShelf(Guid pickupSlotId, Guid updatedBy)
    {
        PickupSlotId = pickupSlotId;
        Status = ServingJobStatus.OnShelf;
        Touch(updatedBy);
    }

    public void MarkCollected(Guid updatedBy)
    {
        Status = ServingJobStatus.Collected;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        Touch(updatedBy);
    }

    public void MarkFailed(string reason, Guid updatedBy)
    {
        Status = ServingJobStatus.Failed;
        FailureReason = reason;
        Touch(updatedBy);
    }

    // Staff cho chạy lại job Failed: về hàng đợi, GIỮ TrayId (món đã gắp còn trên khay)
    // -> pull kế tiếp tái dùng khay, robot gắp tiếp phần thiếu (resume, không replay).
    public void Requeue(Guid updatedBy)
    {
        Status = ServingJobStatus.Queued;
        FailureReason = null;
        Touch(updatedBy);
    }

    // Staff tự đặt tay phần món còn thiếu -> khay coi như ráp xong, đủ điều kiện lên kệ.
    public void MarkAssembledManually(Guid updatedBy)
    {
        Status = ServingJobStatus.Assembling;
        FailureReason = null;
        AcknowledgedAtUtc ??= DateTimeOffset.UtcNow;
        Touch(updatedBy);
    }

    // Gỡ khay khỏi job (khi manager force-release khay của job Failed đã dọn đồ).
    public void ClearTray(Guid updatedBy)
    {
        TrayId = null;
        Touch(updatedBy);
    }

    public void Cancel(Guid updatedBy)
    {
        Status = ServingJobStatus.Cancelled;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        Touch(updatedBy);
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }

    private void Touch(Guid updatedBy)
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
