using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.SlotConfiguration.Entity;

/// <summary>
/// Cấu hình lane robot GẮP theo phiên (Meal/Session): lane (kệ gravity) -&gt; món.
/// KHÁC PickupSlot (ô HS lấy). Dùng để robot biết món X nằm ở lane nào.
/// </summary>
public class SlotConfiguration : Entity<Guid>, IAuditableEntity<Guid>
{
    private SlotConfiguration()
    {
    }

    public required Guid SessionId { get; set; }   // phiên phục vụ (Meal/Session)
    public required Guid DishId { get; set; }
    public Guid? RobotArmId { get; set; }
    public required string LaneCode { get; set; }  // mã lane trên kệ gravity (vd "L1-A")
    public int Capacity { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static SlotConfiguration Create(
        Guid sessionId,
        Guid dishId,
        string laneCode,
        int capacity,
        Guid createdBy,
        Guid? robotArmId = null)
    {
        return new SlotConfiguration
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            DishId = dishId,
            LaneCode = laneCode,
            Capacity = capacity,
            RobotArmId = robotArmId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void Update(Guid dishId, string laneCode, int capacity, Guid updatedBy)
    {
        DishId = dishId;
        LaneCode = laneCode;
        Capacity = capacity;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
