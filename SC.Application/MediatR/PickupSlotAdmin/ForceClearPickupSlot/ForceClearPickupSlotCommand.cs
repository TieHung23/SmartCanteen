using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.PickupSlotAdmin.ForceClearPickupSlot;

/// <summary>
/// Dọn ô no-show: khách quá hạn không lấy -> staff/manager dọn ô.
/// Ô -> Empty; đơn (nếu đang ReadyForPickup) -> Expired + ghi history; job active -> Cancelled.
/// </summary>
public sealed record ForceClearPickupSlotCommand(Guid Id) : ICommand<ForceClearPickupSlotResponse>;

public sealed record ForceClearPickupSlotResponse(
    Guid Id, string Code, Guid? ExpiredOrderId);
