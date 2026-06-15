using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using CartAggregateRoot = SC.Domain.Domain.Cart.AggregateRoot.Cart;

namespace SC.Application.MediatR.Cart.GetCart;

internal sealed class GetCartQueryHandler(
    IGenericRepository<CartAggregateRoot, Guid> cartRepository,
    ICurrentUserService currentUserService,
    ILogger<GetCartQueryHandler> logger)
    : IQueryHandler<GetCartQuery, CartResponse>
{
    public async Task<Result<CartResponse>> Handle(
        GetCartQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            var cart = await cartRepository
                .GetQueryable(x => x.UserId == userId)
                .SingleOrDefaultAsync(cancellationToken);

            if (cart is null)
            {
                return Result.Success(
                    new CartResponse
                    {
                        Data = new CartData(),
                        Version = 0
                    },
                    "Cart retrieved successfully.");
            }

            return Result.Success(
                new CartResponse
                {
                    Id = cart.Id,
                    Data = CartJson.Deserialize(cart.DataJson),
                    Version = cart.Version,
                    UpdatedAtUtc = cart.UpdatedAtUtc ?? cart.CreatedAtUtc
                },
                "Cart retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving cart for user {UserId}", currentUserService.UserId);
            return Result.Failure<CartResponse>(
                Error.ServerError,
                "An error occurred while retrieving the cart.");
        }
    }
}
