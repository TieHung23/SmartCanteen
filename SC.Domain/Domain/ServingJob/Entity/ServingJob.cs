using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.ServingJob.Enum;

namespace SC.Domain.Domain.ServingJob.Entity;

/// <summary>
/// Hàng đợi phục vụ (PUSH/BUFFER). BE tạo job khi thanh toán xong, bind Order↔Tray,
/// đẩy (SignalR) cho robot service. Robot service báo trạng thái ngược lại.
/// </summary>
public class ServingJob : Entity<Guid>, IAuditableEntity<Guid>
{
    private ServingJob()
    {
    }

    public required Guid OrderId { get; set; }
    public Guid? TrayId { get; set; }
    public Guid? RobotArmId { get; set; }
    public Guid? PickupSlotId { get; set; }

    public ServingJobStatus Status { get; set; } = ServingJobStatus.Queued;

    public DateTimeOffset? PushedAtUtc { get; set; }
    public DateTimeOffset? AcknowledgedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? FailureReason { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

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

    public void MarkPushed(Guid? robotArmId, Guid updatedBy)
    {
        RobotArmId = robotArmId;
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

    public void Cancel(Guid updatedBy)
    {
        Status = ServingJobStatus.Cancelled;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        Touch(updatedBy);
    }

    private void Touch(Guid updatedBy)
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
