using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Tray.ForceReleaseTray;

/// <summary>
/// Gỡ kẹt (ops recovery): khay dính Reserved/InUse do đơn hủy bất thường
/// -> trả về Available + ping robot (khay rảnh có thể phục vụ đơn đang chờ).
/// </summary>
public sealed record ForceReleaseTrayCommand(Guid Id) : ICommand<ForceReleaseTrayResponse>;

public sealed record ForceReleaseTrayResponse(Guid Id, string Code, string Status);
