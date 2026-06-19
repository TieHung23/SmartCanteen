using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.AdminCommandAudit.Enum;

namespace SC.Domain.Domain.AdminCommandAudit.Entity;

public class AdminCommandAudit : Entity<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private AdminCommandAudit() { }

    public AdminCommandType CommandType { get; private set; }
    public Guid? RobotArmId { get; private set; }
    public string? ParametersJson { get; private set; }
    public string? Result { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static AdminCommandAudit Create(AdminCommandType commandType, Guid issuedBy, Guid? robotArmId = null, string? parametersJson = null, string? result = null)
    {
        return new AdminCommandAudit { Id = Guid.NewGuid(), CommandType = commandType, RobotArmId = robotArmId, ParametersJson = parametersJson, Result = result, CreatedAtUtc = DateTimeOffset.UtcNow, CreatedBy = issuedBy, UpdatedBy = issuedBy };
    }

    public void SoftDelete() { IsDeleted = true; DeletedAtUtc = DateTimeOffset.UtcNow; }
}
