using SC.Contract.Abstraction.Message;
using SC.Domain.Domain.User.Enum;

namespace SC.Application.MediatR.Auth.UpdateProfile;

public record UpdateProfileCommand(
    string Name,
    string? ImgUrl = null,
    DateOnly? DateOfBirth = null,
    string? MajorOrClass = null,
    string? PhoneNumber = null,
    string? Address = null,
    Gender? Gender = null) : ICommand<UpdateProfileResponse>;
