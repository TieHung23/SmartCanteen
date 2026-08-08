using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Robot.ClaimServingJob;

/// <summary>
/// Đánh dấu ServingJob của 1 order từ <c>Queued</c> -> <c>Pushed</c> theo OrderId, KHÔNG đi qua
/// PullNextJob (nên KHÔNG bị khóa giờ ca). Dùng cho Unity/ScenarioPlayer test bằng JSON cứng:
/// nhờ được Pushed, report <c>PickStarted</c> kế tiếp mới đẩy tiếp sang <c>Assembling</c> -> trạng
/// thái job chạy đúng nấc như luồng thật. CHỈ dùng ở môi trường test/demo (hub gate bằng cấu hình
/// <c>Serving:AllowManualClaim</c>).
/// </summary>
public sealed record ClaimServingJobCommand(Guid OrderId) : ICommand<ClaimServingJobResponse>;

public sealed record ClaimServingJobResponse(Guid JobId, Guid OrderId, string Status);
