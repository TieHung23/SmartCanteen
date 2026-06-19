namespace SC.Domain.Domain.AdminCommandAudit.Enum;

/// <summary>
/// Loại lệnh admin tác động lên robot fleet (đều được ghi audit).
/// </summary>
public enum AdminCommandType
{
    StartRobot = 0,
    StopRobot = 1,
    ResetError = 2,
    ChangeIp = 3,
    ReassignSlot = 4,
    Other = 5
}
