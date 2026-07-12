using FluentValidation;

namespace SC.Application.MediatR.PickupSlotAdmin.CreatePickupSlots;

public class CreatePickupSlotsCommandValidator : AbstractValidator<CreatePickupSlotsCommand>
{
    private const int MaxBulk = 100;

    public CreatePickupSlotsCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Code)
                       || (!string.IsNullOrWhiteSpace(x.Prefix) && x.From.HasValue && x.To.HasValue))
            .WithMessage($"Provide Code, or Prefix+From+To (max {MaxBulk} per request).");

        RuleFor(x => x.Code)
            .MaximumLength(50);

        When(x => string.IsNullOrWhiteSpace(x.Code), () =>
        {
            RuleFor(x => x.Prefix).NotEmpty().MaximumLength(20);
            RuleFor(x => x.From).NotNull().GreaterThanOrEqualTo(0);
            RuleFor(x => x.To).NotNull();
            RuleFor(x => x)
                .Must(x => x.From.HasValue && x.To.HasValue
                           && x.To >= x.From && x.To - x.From + 1 <= MaxBulk)
                .WithMessage($"To must be >= From and range must not exceed {MaxBulk}.");
        });
    }
}
