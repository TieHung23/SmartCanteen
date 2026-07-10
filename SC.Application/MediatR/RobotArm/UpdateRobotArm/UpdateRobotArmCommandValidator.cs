using System.Net;
using FluentValidation;

namespace SC.Application.MediatR.RobotArm.UpdateRobotArm;

public class UpdateRobotArmCommandValidator : AbstractValidator<UpdateRobotArmCommand>
{
    public UpdateRobotArmCommandValidator()
    {
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
