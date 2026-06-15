using SC.Domain.Domain.User.Enum;

namespace SC.Api.Models.Auth;

public sealed class UpdateProfileFormRequest
{
    public string Name { get; init; } = string.Empty;

    public DateOnly? DateOfBirth { get; init; }

    public string? MajorOrClass { get; init; }

    public string? PhoneNumber { get; init; }

    public string? Address { get; init; }

    public Gender? Gender { get; init; }

    public IFormFile? Image { get; init; }
}
