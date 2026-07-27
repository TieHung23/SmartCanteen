using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.ShelfStockAdmin.GetShelfStockDetail;

/// <summary>Chi tiết 1 bản ghi tồn kệ (món↔lane↔số lượng của 1 ca), kèm tên món/lane/ca cho FE.</summary>
public sealed record GetShelfStockDetailQuery(Guid Id) : IQuery<GetShelfStockDetailResponse>;

public sealed record GetShelfStockDetailResponse(
    Guid Id,
    Guid SessionId,
    string? SessionName,
    Guid DishId,
    string? DishName,
    Guid? SlotConfigurationId,
    string? LaneCode,
    int Quantity,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
