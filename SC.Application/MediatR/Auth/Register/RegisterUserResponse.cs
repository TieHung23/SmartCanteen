using SC.Domain.Domain.User.Enum;

namespace SC.Application.MediatR.Auth.Register;

public record RegisterUserResponse(
    Guid UserId,
    UserCategory Category,
    bool RequiresEmailVerification);
