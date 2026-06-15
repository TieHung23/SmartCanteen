using SC.Application.MediatR.Cart;
using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Cart.UpdateCart;

public sealed class UpdateCartCommand : ICommand<CartResponse>
{
    public CartData? Data { get; set; }
    public long ExpectedVersion { get; set; }
}
