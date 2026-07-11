using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Domain.User.Enum;

namespace SC.Application.MediatR.User.Manager.GetUsers;

public class GetManagerUsersQuery : PaginationParams, IQuery<PaginatedList<ManagerUserListItem>>
{
    public string? Search { get; set; }
    public AccountStatus? Status { get; set; }
    public Role? Role { get; set; }
}

public record ManagerUserListItem(
    Guid Id,
    string Name,
    string Email,
    string? ImgUrl,
    Role Role,
    AccountStatus Status,
    string? StatusReason,
    bool EmailVerified,
    string? StudentId,
    string? MajorOrClass,
    string? PhoneNumber,
    decimal BalanceAmount,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAtUtc);
