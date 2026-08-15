using SC.Contract.Abstraction.Message;
using SC.Domain.Domain.User.Enum;

namespace SC.Application.MediatR.Auth.Register;

public record RegisterUserCommand(
    string Name,
    string Email,
    string Password,
    UserCategory Category,
    string? StudentId = null,
    DateOnly? DateOfBirth = null,
    string? MajorOrClass = null,
    string? PhoneNumber = null,
    string? Address = null,
    Gender? Gender = null) : ICommand<RegisterUserResponse>;
