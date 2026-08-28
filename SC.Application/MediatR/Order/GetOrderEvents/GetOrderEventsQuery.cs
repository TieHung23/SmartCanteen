using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Order.GetOrderEvents;

/// <summary>
/// Lịch sử sự kiện robot của 1 ĐƠN — gom MỌI serving job của đơn (1 đơn có thể nhiều job do requeue).
/// RobotEventLog gắn trực tiếp OrderId nên lấy 1 lần theo đơn, khỏi cần jobId.
/// <paramref name="Type"/>: lọc theo loại sự kiện (vd "Error", "PlaceCompleted"); null/rỗng = tất cả.
/// </summary>
public sealed record GetOrderEventsQuery(Guid OrderId, string? Type = null)
    : IQuery<GetOrderEventsResponse>;

public sealed record GetOrderEventsResponse(Guid OrderId, int Count, IReadOnlyList<OrderEventDto> Events);

/// <param name="ServingJobId">Sự kiện thuộc job nào (phân biệt các lượt requeue của cùng đơn).</param>
/// <param name="EventType">PickStarted / PickCompleted / PlaceCompleted / Error / Recovered / JobReceived / ...</param>
/// <param name="Station">Mã tay robot (S1/S2/S3) — null cho event mức job.</param>
public sealed record OrderEventDto(
    Guid? ServingJobId,
    string EventType,
    Guid? DishId,
    string? DishName,
    Guid? RobotArmId,
    string? Station,
    string? Message,
    DateTimeOffset OccurredAt);
