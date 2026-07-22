using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.ShelfStockAdmin.RefillShelfStock;

/// <summary>
/// Staff XÁC NHẬN đã nạp thêm hộp vào lane (BR-152: refill phải được staff confirm
/// thì số lượng mới usable). Chính lời gọi API này = hành động confirm (actor ghi ở UpdatedBy).
/// </summary>
public sealed record RefillShelfStockCommand(Guid Id, int Quantity) : ICommand<RefillShelfStockResponse>;

public sealed record RefillShelfStockResponse(Guid Id, Guid DishId, int QuantityAfter);
