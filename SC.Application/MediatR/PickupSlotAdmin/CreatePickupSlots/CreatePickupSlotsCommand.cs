using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.PickupSlotAdmin.CreatePickupSlots;

/// <summary>
/// Manager đăng ký ô kệ. Đơn lẻ: Code="SLOT13". Hàng loạt: Prefix="SLOT", From=13, To=24.
/// </summary>
public sealed record CreatePickupSlotsCommand(
    string? Code,
    string? Prefix,
    int? From,
    int? To) : ICommand<CreatePickupSlotsResponse>;

public sealed record CreatePickupSlotsResponse(
    IReadOnlyList<string> CreatedCodes, IReadOnlyList<string> SkippedCodes);
