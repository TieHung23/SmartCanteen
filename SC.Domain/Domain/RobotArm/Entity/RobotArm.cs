using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.RobotArm.Enum;

namespace SC.Domain.Domain.RobotArm.Entity;

/// <summary>
/// Đăng ký 1 cánh tay FAIRINO FR3 trong fleet. Mỗi arm 1 IP DUY NHẤT (mặc định đều .2 -&gt; phải đổi).
/// </summary>
public class RobotArm : Entity<Guid>, IAuditableEntity<Guid>
{
    private RobotArm()
    {
    }

    public required string Code { get; set; }     // vd "FR3-S1"
    public string? Name { get; set; }
    public required string IpAddress { get; set; }
    public int StationIndex { get; set; }         // trạm/lane mà arm phục vụ
    public RobotArmStatus Status { get; set; } = RobotArmStatus.Offline;
    public DateTimeOffset? LastHeartbeatUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

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
        if (Status == RobotArmStatus.Offline)
            Status = RobotArmStatus.Idle;
        Touch(updatedBy);
    }

    public void ChangeIp(string ipAddress, Guid updatedBy)
    {
        IpAddress = ipAddress;
        Touch(updatedBy);
    }

    private void Touch(Guid updatedBy)
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
