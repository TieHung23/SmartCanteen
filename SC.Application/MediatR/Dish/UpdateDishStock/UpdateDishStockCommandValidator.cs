using FluentValidation;

namespace SC.Application.MediatR.Dish.UpdateDishStock;

public class UpdateDishStockCommandValidator : AbstractValidator<UpdateDishStockCommand>
{
    public UpdateDishStockCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0);
    }
}
