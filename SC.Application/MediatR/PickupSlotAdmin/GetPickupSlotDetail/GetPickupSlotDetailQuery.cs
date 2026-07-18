using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.PickupSlotAdmin.GetPickupSlotDetail;

/// <summary>
/// Chi tiết 1 ô kệ: đang giữ đơn nào, giữ bao lâu rồi (để soi no-show) + lịch sử các job từng đặt vào ô.
/// </summary>
public sealed record GetPickupSlotDetailQuery(Guid Id) : IQuery<GetPickupSlotDetailResponse>;

public sealed record SlotJobDto(
    Guid JobId,
    Guid OrderId,
    string JobStatus,
    string? OrderStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record GetPickupSlotDetailResponse(
    Guid Id,
    string Code,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    Guid? OrderId,
    string? OrderStatus,
    DateTimeOffset? OccupiedSinceUtc,   // lúc staff đặt đơn vào ô
    int? HeldMinutes,                   // đã giữ bao nhiêu phút (null nếu ô trống)
    SlotJobDto? CurrentJob,
    IReadOnlyList<SlotJobDto> History);
