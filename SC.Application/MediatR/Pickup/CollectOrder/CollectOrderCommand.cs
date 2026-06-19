using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Pickup.CollectOrder;

/// <summary>
/// HS quét QR pickup tới lấy. Dò ngược OrderId -&gt; ô kệ, giải phóng ô + khay, đóng job + order.
/// (QR pickup mã hoá OrderId; có thể thay bằng token resolve ra OrderId sau.)
/// </summary>
public sealed record CollectOrderCommand(Guid OrderId)
    : ICommand<CollectOrderResponse>;

public sealed record CollectOrderResponse(Guid OrderId, string? SlotCode);
