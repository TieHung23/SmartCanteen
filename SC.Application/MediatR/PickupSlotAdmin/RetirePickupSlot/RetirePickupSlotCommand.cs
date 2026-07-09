using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.PickupSlotAdmin.RetirePickupSlot;

/// <summary>Ô hỏng -> rút khỏi kệ (soft-delete). Chỉ khi ô đang Empty.</summary>
public sealed record RetirePickupSlotCommand(Guid Id) : ICommand<RetirePickupSlotResponse>;

public sealed record RetirePickupSlotResponse(Guid Id, string Code);
