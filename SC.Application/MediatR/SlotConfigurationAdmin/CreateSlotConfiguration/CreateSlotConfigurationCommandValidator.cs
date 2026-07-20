using FluentValidation;

namespace SC.Application.MediatR.SlotConfigurationAdmin.CreateSlotConfiguration;

public class CreateSlotConfigurationCommandValidator : AbstractValidator<CreateSlotConfigurationCommand>
{
    public CreateSlotConfigurationCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.DishId).NotEmpty();

        RuleFor(x => x.LaneCode)
            .NotEmpty()
            .MaximumLength(20)
            .Matches(@"^[A-Za-z0-9]+_L[1-3]$")
            .WithMessage("LaneCode must be a lane position code like S1_L1..S3_L3.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0);
    }
}
