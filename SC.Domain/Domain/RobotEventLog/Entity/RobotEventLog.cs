using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.RobotEventLog.Enum;

namespace SC.Domain.Domain.RobotEventLog.Entity;

public class RobotEventLog : Entity<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private RobotEventLog() { }

    public Guid? RobotArmId { get; private set; }
    public Guid? ServingJobId { get; private set; }
    public Guid? OrderId { get; private set; }
    public RobotEventType EventType { get; private set; }
    public string? Message { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static RobotEventLog Create(RobotEventType eventType, Guid createdBy, Guid? robotArmId = null, Guid? servingJobId = null, Guid? orderId = null, string? message = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new RobotEventLog { Id = Guid.NewGuid(), EventType = eventType, RobotArmId = robotArmId, ServingJobId = servingJobId, OrderId = orderId, Message = message, OccurredAtUtc = now, CreatedAtUtc = now, CreatedBy = createdBy, UpdatedBy = createdBy };
    }

    public void SoftDelete() { IsDeleted = true; DeletedAtUtc = DateTimeOffset.UtcNow; }
}
