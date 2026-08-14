using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.ServingJobAdmin.GetServingJobEvents;

/// <summary>Lịch sử sự kiện robot của 1 serving job (để staff/manager xem lỗi xảy ra ở bước/lane nào).</summary>
public sealed record GetServingJobEventsQuery(Guid JobId) : IQuery<GetServingJobEventsResponse>;

public sealed record ServingJobEventDto(
    string EventType,
    Guid? DishId,
    string? DishName,
    Guid? RobotArmId,
    string? Station,
    string? Message,
    DateTimeOffset OccurredAtUtc);

public sealed record GetServingJobEventsResponse(
    Guid JobId,
    Guid OrderId,
    int Total,
    IReadOnlyList<ServingJobEventDto> Events);
