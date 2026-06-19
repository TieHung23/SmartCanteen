using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Robot.CreateServingJob;

/// <summary>
/// Tạo job phục vụ cho 1 order đã thanh toán + đẩy (SignalR) cho robot service (PUSH/BUFFER).
/// Idempotent: nếu order đã có job đang hoạt động thì trả lại job đó.
/// </summary>
public sealed record CreateServingJobCommand(Guid OrderId, Guid? TrayId = null)
    : ICommand<CreateServingJobResponse>;
