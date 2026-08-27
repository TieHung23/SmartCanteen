using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.ServingJobAdmin.FailServingJob;

/// <summary>
/// Staff TUYÊN BỐ 1 job đang dang dở (Queued/Pushed/Assembling) là Failed — dùng khi robot
/// mất kết nối / đơn kẹt chờ mà cần can thiệp. Mở đường vào 2 nút cứu sẵn có
/// (Requeue / ManualComplete — cả hai chỉ nhận job Failed).
/// Nguyên tắc #3: máy KHÔNG tự khai tử job Queued; chỉ NGƯỜI (staff) tuyên bố Failed.
/// </summary>
public sealed record FailServingJobCommand(Guid Id, string? Reason)
    : ICommand<FailServingJobResponse>;

public sealed record FailServingJobResponse(Guid JobId, Guid OrderId, string Status);
