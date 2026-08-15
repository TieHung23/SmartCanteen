using SC.Domain.Domain.User.Enum;

namespace SC.Application.MediatR.Auth.UpdateProfile;

public record UpdateProfileResponse(
    Guid Id,
    string Name,
    string Email,
    string? ImgUrl,
    Role Role,
    UserCategory Category,
    AccountStatus Status,
    bool EmailVerified,
    string? StudentId,
    DateOnly? DateOfBirth,
    string? MajorOrClass,
    string? PhoneNumber,
    string? Address,
    Gender? Gender,
    decimal BalanceAmount,
    DateTimeOffset? LastLoginAt);
