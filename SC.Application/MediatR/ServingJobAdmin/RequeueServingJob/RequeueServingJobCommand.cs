using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.ServingJobAdmin.RequeueServingJob;

/// <summary>
/// Staff cho job Failed chạy lại: về Queued, GIỮ khay (món đã gắp còn trên khay)
/// -> robot pull lại và gắp tiếp phần thiếu (resume, không làm lại từ đầu).
/// </summary>
public sealed record RequeueServingJobCommand(Guid Id) : ICommand<RequeueServingJobResponse>;

public sealed record RequeueServingJobResponse(Guid JobId, Guid OrderId, string Status);
