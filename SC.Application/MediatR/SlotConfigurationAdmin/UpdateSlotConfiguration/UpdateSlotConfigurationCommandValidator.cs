using FluentValidation;

namespace SC.Application.MediatR.SlotConfigurationAdmin.UpdateSlotConfiguration;

public class UpdateSlotConfigurationCommandValidator : AbstractValidator<UpdateSlotConfigurationCommand>
{
    public UpdateSlotConfigurationCommandValidator()
    {
        RuleFor(x => x.DishId).NotEmpty();

        RuleFor(x => x.LaneCode)
            .IsInEnum()
            .WithMessage("LaneCode must be a valid lane (e.g. S1_L1..S3_L3).");

        RuleFor(x => x.Capacity)
            .GreaterThan(0);
    }
}
