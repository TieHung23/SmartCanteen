using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Payment.TopUpWallet;

public class TopUpWalletCommand : ICommand<TopUpWalletResponse>
{
    public decimal AmountVnd { get; set; }
    public int Method { get; set; }
}
