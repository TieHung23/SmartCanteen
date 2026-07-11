using SC.Domain.Domain.User.Enum;

namespace SC.Application.MediatR.User.Manager.ReactivateUser;

public record ReactivateUserResponse(
    Guid UserId,
    AccountStatus Status,
    string Message);
