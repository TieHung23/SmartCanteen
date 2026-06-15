using SC.Application.MediatR.Cart;
using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Cart.GetCart;

public sealed record GetCartQuery : IQuery<CartResponse>;
