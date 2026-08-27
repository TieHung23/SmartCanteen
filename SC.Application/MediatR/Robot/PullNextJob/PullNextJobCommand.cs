using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Robot.PullNextJob;

/// <summary>
/// Robot service KÉO job kế tiếp (hybrid pull). Lấy ServingJob <c>Queued</c> cũ nhất (FIFO theo giờ tạo),
/// claim job (→ <c>Pushed</c>), trả job. Hết job → <c>Job = null</c>.
/// <para>
/// <paramref name="TrayCode"/> (tray-first, #4): khay VẬT LÝ Unity vừa quét. Optional (backward-compatible,
/// null = FIFO cũ). Có khay thì chọn job theo khay để KHÔNG phá FIFO:
/// khay Available → FIFO-oldest job CHƯA có khay; khay Reserved-của-1-job-Queued → CHÍNH job đó (resume).
/// </para>
/// </summary>
public sealed record PullNextJobCommand(string? TrayCode = null) : ICommand<PullNextJobResponse>;
