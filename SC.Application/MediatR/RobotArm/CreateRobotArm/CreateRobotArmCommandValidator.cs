using System.Net;
using FluentValidation;

namespace SC.Application.MediatR.RobotArm.CreateRobotArm;

public class CreateRobotArmCommandValidator : AbstractValidator<CreateRobotArmCommand>
{
    public CreateRobotArmCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(20)
            .Matches("^[A-Za-z0-9_-]+$")
            .WithMessage("Code must contain only letters, numbers, underscore, or hyphen.");

        RuleFor(x => x.IpAddress)
            .NotEmpty()
            .Must(ip => IPAddress.TryParse((ip ?? string.Empty).Trim(), out _))
            .WithMessage("IpAddress is not a valid IP address.");

        RuleFor(x => x.StationIndex)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Name)
            .MaximumLength(100);
    }
}
