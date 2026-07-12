using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.ShelfStockAdmin.CreateShelfStock;

/// <summary>Manager khởi tạo dòng tồn kệ cho món trong 1 session (1 dòng / cặp session+dish).</summary>
public sealed record CreateShelfStockCommand(
    Guid SessionId,
    Guid DishId,
    int Quantity,
    Guid? SlotConfigurationId) : ICommand<CreateShelfStockResponse>;

public sealed record CreateShelfStockResponse(
    Guid Id, Guid SessionId, Guid DishId, int Quantity, Guid? SlotConfigurationId);
