using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Tray.GetTrayDetail;

/// <summary>
/// Chi tiết 1 khay: đang phục vụ job/đơn nào (nếu bận) + lịch sử các job khay từng chở.
/// Tra ngược từ ServingJob.TrayId (nguồn sự thật), không cần cột denormalize trên Trays.
/// </summary>
public sealed record GetTrayDetailQuery(Guid Id) : IQuery<GetTrayDetailResponse>;

/// <summary>1 job khay này phục vụ (dùng cho cả currentJob lẫn history).</summary>
public sealed record TrayJobDto(
    Guid JobId,
    Guid OrderId,
    string JobStatus,
    string? OrderStatus,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PushedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record GetTrayDetailResponse(
    Guid Id,
    string Code,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    TrayJobDto? CurrentJob,
    IReadOnlyList<TrayJobDto> History);
