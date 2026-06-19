using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.AdminCommandAudit.Enum;

namespace SC.Domain.Domain.AdminCommandAudit.Entity;

/// <summary>
/// Audit lệnh admin tác động lên robot fleet (start/stop/reset/đổi IP/gán lại slot).
/// </summary>
public class AdminCommandAudit : Entity<Guid>, IAuditableEntity<Guid>
{
    private AdminCommandAudit()
    {
    }

    public required AdminCommandType CommandType { get; set; }
    public Guid? RobotArmId { get; set; }
    public string? ParametersJson { get; set; }
    public string? Result { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static AdminCommandAudit Create(
        AdminCommandType commandType,
        Guid issuedBy,
        Guid? robotArmId = null,
        string? parametersJson = null,
        string? result = null)
    {
        return new AdminCommandAudit
        {
            Id = Guid.NewGuid(),
            CommandType = commandType,
            RobotArmId = robotArmId,
            ParametersJson = parametersJson,
            Result = result,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = issuedBy,
            UpdatedBy = issuedBy
        };
    }
}
