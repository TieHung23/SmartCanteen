using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.ServingJobAdmin.GetServingJobs;

/// <summary>
/// Manager/staff theo dõi job robot: lọc theo status (Queued/Pushed/Assembling/OnShelf/Collected/Failed/Cancelled)
/// và theo sessionId (chỉ job của các order thuộc session đó),
/// mặc định trả các job MỚI NHẤT (take giới hạn, tối đa 200).
/// </summary>
public sealed record GetServingJobsQuery(string? Status, Guid? SessionId = null, int Take = 50)
    : IQuery<GetServingJobsResponse>;

public sealed record ServingJobDto(
    Guid JobId,
    Guid OrderId,
    string Status,
    Guid? TrayId,
    string? TrayCode,
    Guid? PickupSlotId,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PushedAtUtc,
    DateTimeOffset? AcknowledgedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record GetServingJobsResponse(int Total, IReadOnlyList<ServingJobDto> Jobs);
