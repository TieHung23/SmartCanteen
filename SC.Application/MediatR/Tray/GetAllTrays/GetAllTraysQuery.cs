using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Tray.GetAllTrays;

/// <summary>Manager xem pool khay: đếm theo trạng thái + khay nào đang giữ đơn nào.</summary>
public sealed record GetAllTraysQuery : IQuery<GetAllTraysResponse>;

public sealed record TrayDto(
    Guid Id, string Code, string Status, Guid? CurrentOrderId, DateTimeOffset? UpdatedAtUtc);

public sealed record GetAllTraysResponse(
    int Available, int Reserved, int InUse, IReadOnlyList<TrayDto> Trays);
