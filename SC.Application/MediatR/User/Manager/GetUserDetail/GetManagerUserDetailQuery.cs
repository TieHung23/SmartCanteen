using SC.Contract.Abstraction.Message;
using SC.Domain.Domain.User.Enum;

namespace SC.Application.MediatR.User.Manager.GetUserDetail;

public record GetManagerUserDetailQuery(Guid UserId) : IQuery<ManagerUserDetailResponse>;

public record ManagerUserDetailResponse(
    Guid Id,
    string Name,
    string Email,
    string? ImgUrl,
    Role Role,
    UserCategory Category,
    AccountStatus Status,
    string? StatusReason,
    bool EmailVerified,
    string? StudentId,
    DateOnly? DateOfBirth,
    string? MajorOrClass,
    string? PhoneNumber,
    string? Address,
    Gender? Gender,
    decimal BalanceAmount,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
