using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.PickupSlotAdmin.GetAllPickupSlots;

/// <summary>Manager xem kệ pickup: ô nào trống / đang giữ đơn nào từ lúc nào.</summary>
public sealed record GetAllPickupSlotsQuery : IQuery<GetAllPickupSlotsResponse>;

public sealed record PickupSlotDto(
    Guid Id, string Code, string Status, Guid? OrderId, Guid? TrayId, DateTimeOffset? UpdatedAtUtc);

public sealed record GetAllPickupSlotsResponse(
    int Empty, int Occupied, IReadOnlyList<PickupSlotDto> Slots);
