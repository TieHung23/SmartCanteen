using FluentValidation;

namespace SC.Application.MediatR.SlotConfigurationAdmin.UpdateSlotConfiguration;

public class UpdateSlotConfigurationCommandValidator : AbstractValidator<UpdateSlotConfigurationCommand>
{
    public UpdateSlotConfigurationCommandValidator()
    {
        RuleFor(x => x.DishId).NotEmpty();

        RuleFor(x => x.LaneCode)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9_-]+$")
            .WithMessage("LaneCode must contain only letters, numbers, underscore, or hyphen.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0);
    }
}
