using SC.Domain.Domain.User.Enum;

namespace SC.Application.MediatR.User.Manager.UpdateAccountStatus;

public record UpdateUserAccountStatusResponse(
    Guid UserId,
    AccountStatus Status,
    int RevokedRefreshTokenCount,
    string Reason,
    string Message);
