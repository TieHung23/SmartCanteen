using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Pickup.AssignPickupSlot;

/// <summary>
/// Gán khay (đã ráp xong) của 1 order vào 1 ô kệ pickup. Gọi khi staff/sensor xác nhận khay tới ô.
/// </summary>
public sealed record AssignPickupSlotCommand(string SlotCode, Guid OrderId, string TrayCode)
    : ICommand<AssignPickupSlotResponse>;

public sealed record AssignPickupSlotResponse(Guid SlotId, string SlotCode, Guid OrderId, Guid TrayId);
