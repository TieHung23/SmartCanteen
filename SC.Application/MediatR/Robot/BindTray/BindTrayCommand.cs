using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Robot.BindTray;

/// <summary>
/// Edge quét mã khay VẬT LÝ ở đầu quy trình ráp rồi gọi API này để bind khay đó vào job
/// (thay cho auto-assign ở PullNextJob). Đảm bảo ServingJob.TrayId = khay thật → pickup không lệch.
/// </summary>
public sealed record BindTrayCommand(Guid JobId, string TrayCode) : ICommand<BindTrayResponse>;

public sealed record BindTrayResponse(
    Guid JobId, Guid OrderId, Guid TrayId, string TrayCode, string Status);
