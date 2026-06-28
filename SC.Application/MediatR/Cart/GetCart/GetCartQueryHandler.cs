using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using CartAggregateRoot = SC.Domain.Domain.Cart.AggregateRoot.Cart;

namespace SC.Application.MediatR.Cart.GetCart;

internal sealed class GetCartQueryHandler(
    IGenericRepository<CartAggregateRoot, Guid> cartRepository,
    IGenericRepository<SC.Domain.Domain.Session.AggregateRoot.Session, Guid> sessionRepository,
    IGenericRepository<SC.Domain.Domain.Dish.AggregateRoot.Dish, Guid> dishRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IWalletDomainService walletDomainService,
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
                .FindSingleAsync(x => x.UserId == userId, cancellationToken);

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

            var cartData = CartJson.Deserialize(cart.DataJson);
            var activeCartData = await RemoveExpiredSessionsAsync(
                cartData,
                cancellationToken);

            await EnrichCartItemsAsync(activeCartData, cancellationToken);

            if (activeCartData.Sessions.Count != cartData.Sessions.Count)
            {
                await unitOfWork.BeginTransactionAsync(cancellationToken);
                await walletDomainService.LockUserAsync(userId, cancellationToken);

                var currentCart = await cartRepository
                    .FindSingleAsync(x => x.UserId == userId, cancellationToken);

                if (currentCart is not null && currentCart.Version == cart.Version)
                {
                    currentCart.Update(CartJson.Serialize(activeCartData), userId);
                    cartRepository.Update(currentCart);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    await unitOfWork.CommitAsync(cancellationToken);
                    cart = currentCart;
                    cartData = activeCartData;
                }
                else
                {
                    await unitOfWork.RollbackAsync(cancellationToken);
                    cartData = currentCart is null
                        ? new CartData()
                        : CartJson.Deserialize(currentCart.DataJson);
                    cart = currentCart;
                }
            }

            return Result.Success(
                new CartResponse
                {
                    Id = cart?.Id,
                    Data = cartData,
                    Version = cart?.Version ?? 0,
                    UpdatedAtUtc = cart is null ? null : cart.UpdatedAtUtc ?? cart.CreatedAtUtc
                },
                "Cart retrieved successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error retrieving cart for user {UserId}", currentUserService.UserId);
            return Result.Failure<CartResponse>(
                Error.ServerError,
                "An error occurred while retrieving the cart.");
        }
    }

    private async Task<CartData> RemoveExpiredSessionsAsync(
        CartData cartData,
        CancellationToken cancellationToken)
    {
        if (cartData.Sessions.Count == 0)
        {
            return cartData;
        }

        var now = DateTimeOffset.UtcNow;
        var sessionIds = cartData.Sessions.Select(x => x.SessionId).Distinct().ToList();
        var availableSessions = await sessionRepository
            .FindListAsync(x =>
                sessionIds.Contains(x.Id)
                && !x.IsDeleted
                && x.IsActive
                && x.AvailableForOrder >= now,
                cancellationToken);
        var availableSessionIds = availableSessions.Select(x => x.Id).ToList();

        var availableSessionIdSet = availableSessionIds.ToHashSet();
        return new CartData
        {
            Sessions = cartData.Sessions
                .Where(x => availableSessionIdSet.Contains(x.SessionId))
                .ToList()
        };
    }

    private async Task EnrichCartItemsAsync(CartData cartData, CancellationToken cancellationToken)
    {
        var dishIds = cartData.Sessions
            .Where(s => s.Items is { Count: > 0 })
            .SelectMany(s => s.Items!)
            .Select(i => i.DishId)
            .Distinct()
            .ToList();

        if (dishIds.Count == 0)
            return;

        var dishes = await dishRepository
            .FindListAsync(d => dishIds.Contains(d.Id), cancellationToken);

        var dishMap = dishes.ToDictionary(d => d.Id);

        foreach (var session in cartData.Sessions)
        {
            if (session.Items is null)
                continue;

            foreach (var item in session.Items)
            {
                if (dishMap.TryGetValue(item.DishId, out var dish))
                {
                    item.DishName = dish.Name;
                    item.ImgUrl = dish.ImgUrl;
                }
            }
        }
    }
}
