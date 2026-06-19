namespace SC.Domain.Domain.RobotArm.Enum;

/// <summary>
/// Trạng thái cánh tay robot FAIRINO FR3 trong fleet (mỗi arm 1 IP riêng).
/// </summary>
public enum RobotArmStatus
{
    Offline = 0,      // không kết nối / chưa heartbeat
    Idle = 1,         // online, rảnh
    Busy = 2,         // đang thực hiện job
    Error = 3,        // có alarm (vd overrun) -> cần ResetAllError/staff
    Maintenance = 4   // tạm ngừng để bảo trì
}
