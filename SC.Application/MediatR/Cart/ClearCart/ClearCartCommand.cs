using SC.Application.MediatR.Cart;
using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Cart.ClearCart;

public sealed record ClearCartCommand(long ExpectedVersion) : ICommand<CartResponse>;
