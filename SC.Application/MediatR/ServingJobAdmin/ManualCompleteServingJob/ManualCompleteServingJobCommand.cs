using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.ServingJobAdmin.ManualCompleteServingJob;

/// <summary>
/// Staff TỰ ĐẶT TAY phần món còn thiếu của job Failed (BR: arm fails mid-order,
/// staff must manually complete). Job -> Assembling: khay coi như ráp xong,
/// đủ điều kiện lên kệ như bình thường. Ghi RobotEventLog phân biệt "manual".
/// </summary>
public sealed record ManualCompleteServingJobCommand(Guid Id, string? Note)
    : ICommand<ManualCompleteServingJobResponse>;

public sealed record ManualCompleteServingJobResponse(Guid JobId, Guid OrderId, string Status);
