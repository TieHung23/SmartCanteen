using FluentValidation;

namespace SC.Application.MediatR.Payment.TopUpWallet;

public class TopUpWalletCommandValidator : AbstractValidator<TopUpWalletCommand>
{
    public TopUpWalletCommandValidator()
    {
        RuleFor(x => x.AmountVnd)
            .GreaterThan(0);

        RuleFor(x => x.Method)
            .InclusiveBetween(1, 4);
    }
}
