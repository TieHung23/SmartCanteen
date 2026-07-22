using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Robot.ReportServingStatus;

/// <summary>
/// Robot service báo tiến độ/kết quả 1 job về BE (gọi từ RobotHub.ReportStatus).
/// </summary>
public sealed record ReportServingStatusCommand(
    Guid OrderId,
    string State,
    string? Station,
    string? Message,
    Guid? TrayId,
    Guid? DishId = null) : ICommand<ReportServingStatusResponse>;

public sealed record ReportServingStatusResponse(Guid OrderId, string State, string ServingJobStatus);
