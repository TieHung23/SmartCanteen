using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Robot.PullNextJob;

/// <summary>
/// Robot service KÉO job kế tiếp (hybrid pull). Lấy ServingJob <c>Queued</c> cũ nhất (FIFO theo giờ tạo),
/// gán 1 khay trống (lazy), claim job (→ <c>Pushed</c>), trả job. Hết job hoặc hết khay → <c>Job = null</c>.
/// </summary>
public sealed record PullNextJobCommand : ICommand<PullNextJobResponse>;
