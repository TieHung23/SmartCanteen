using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.SlotConfiguration.Entity;

public class SlotConfiguration : Entity<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private SlotConfiguration() { }

    public Guid SessionId { get; private set; }
    public Guid DishId { get; private set; }
    public Guid? RobotArmId { get; private set; }
    public string LaneCode { get; private set; } = string.Empty;
    public int Capacity { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static SlotConfiguration Create(Guid sessionId, Guid dishId, string laneCode, int capacity, Guid createdBy, Guid? robotArmId = null)
    {
        return new SlotConfiguration { Id = Guid.NewGuid(), SessionId = sessionId, DishId = dishId, LaneCode = laneCode, Capacity = capacity, RobotArmId = robotArmId, CreatedAtUtc = DateTimeOffset.UtcNow, CreatedBy = createdBy, UpdatedBy = createdBy };
    }

    public void Update(Guid dishId, string laneCode, int capacity, Guid? robotArmId, Guid updatedBy) { DishId = dishId; LaneCode = laneCode; Capacity = capacity; RobotArmId = robotArmId; Touch(updatedBy); }
    public void SoftDelete() { IsDeleted = true; DeletedAtUtc = DateTimeOffset.UtcNow; }
    private void Touch(Guid updatedBy) { UpdatedAtUtc = DateTimeOffset.UtcNow; UpdatedBy = updatedBy; }
}
