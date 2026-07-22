using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.ShelfStockAdmin.GetShelfStocks;

/// <summary>Staff/Manager xem tồn kho trên kệ của 1 session (món nào lane nào còn bao nhiêu hộp).</summary>
public sealed record GetShelfStocksQuery(Guid SessionId) : IQuery<GetShelfStocksResponse>;

public sealed record ShelfStockDto(
    Guid Id, Guid SessionId, Guid DishId, string? DishName,
    Guid? SlotConfigurationId, string? LaneCode, int Quantity);

public sealed record GetShelfStocksResponse(IReadOnlyList<ShelfStockDto> Stocks);
