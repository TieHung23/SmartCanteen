using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Robot.AutoBindTray;

/// <summary>
/// Unity (digital twin) không có webcam quét mã khay → nhờ BE TỰ CHỌN 1 khay Available bất kỳ
/// rồi bind vào job (mô phỏng "quét được khay nào thì dùng khay đó"). Hết khay Available -&gt; fail
/// (Unity KHÔNG phục vụ, giống Edge HoldForStaff khi bind khay thất bại).
/// </summary>
public sealed record AutoBindTrayCommand(Guid JobId) : ICommand<AutoBindTrayResponse>;
