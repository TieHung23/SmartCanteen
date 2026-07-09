using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.RobotArm.Enum;

namespace SC.Domain.Domain.RobotArm.Entity;

public class RobotArm : Entity<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private RobotArm() { }

    public string Code { get; private set; } = string.Empty;
    public string? Name { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public int StationIndex { get; private set; }
    public RobotArmStatus Status { get; private set; } = RobotArmStatus.Offline;
    public DateTimeOffset? LastHeartbeatUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static RobotArm Create(string code, string ipAddress, int stationIndex, Guid createdBy, string? name = null)
    {
        return new RobotArm
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            IpAddress = ipAddress,
            StationIndex = stationIndex,
            Status = RobotArmStatus.Offline,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void UpdateStatus(RobotArmStatus status, Guid updatedBy)
    {
        Status = status;
        Touch(updatedBy);
    }

    public void Heartbeat(Guid updatedBy)
    {
        LastHeartbeatUtc = DateTimeOffset.UtcNow;
        if (Status == RobotArmStatus.Offline) Status = RobotArmStatus.Idle;
        Touch(updatedBy);
    }

    public void ChangeIp(string ipAddress, Guid updatedBy)
    {
        IpAddress = ipAddress;
        Touch(updatedBy);
    }

    public void Update(string? name, string ipAddress, int stationIndex, Guid updatedBy)
    {
        Name = name;
        IpAddress = ipAddress;
        StationIndex = stationIndex;
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
