using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.RobotEventLog.Enum;

namespace SC.Domain.Domain.RobotEventLog.Entity;

/// <summary>
/// Nhật ký sự kiện robot service báo về BE (audit + realtime). Append-only.
/// </summary>
public class RobotEventLog : Entity<Guid>, IAuditableEntity<Guid>
{
    private RobotEventLog()
    {
    }

    public Guid? RobotArmId { get; set; }
    public Guid? ServingJobId { get; set; }
    public Guid? OrderId { get; set; }
    public required RobotEventType EventType { get; set; }
    public string? Message { get; set; }
    public string? PayloadJson { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static RobotEventLog Create(
        RobotEventType eventType,
        Guid createdBy,
        Guid? robotArmId = null,
        Guid? servingJobId = null,
        Guid? orderId = null,
        string? message = null,
        string? payloadJson = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new RobotEventLog
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            RobotArmId = robotArmId,
            ServingJobId = servingJobId,
            OrderId = orderId,
            Message = message,
            PayloadJson = payloadJson,
            OccurredAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }
}
