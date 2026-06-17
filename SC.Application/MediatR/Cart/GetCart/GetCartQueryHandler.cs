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
    IGenericRepository<SC.Domain.Domain.Meal.AggregateRoot.Meal, Guid> mealRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
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

            var cartData = CartJson.Deserialize(cart.DataJson);
            var activeCartData = await RemoveExpiredMealsAsync(
                cartData,
                cancellationToken);

            if (activeCartData.Meals.Count != cartData.Meals.Count)
            {
                await unitOfWork.BeginTransactionAsync(cancellationToken);
                await unitOfWork.LockUserAsync(userId, cancellationToken);

                var currentCart = await cartRepository
                    .GetQueryable(x => x.UserId == userId)
                    .SingleOrDefaultAsync(cancellationToken);

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

    private async Task<CartData> RemoveExpiredMealsAsync(
        CartData cartData,
        CancellationToken cancellationToken)
    {
        if (cartData.Meals.Count == 0)
        {
            return cartData;
        }

        var now = DateTimeOffset.UtcNow;
        var mealIds = cartData.Meals.Select(x => x.MealId).Distinct().ToList();
        var availableMealIds = await mealRepository
            .GetQueryable(x =>
                mealIds.Contains(x.Id)
                && !x.IsDeleted
                && x.IsActive
                && x.AvailableForOrder >= now)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var availableMealIdSet = availableMealIds.ToHashSet();
        return new CartData
        {
            Meals = cartData.Meals
                .Where(x => availableMealIdSet.Contains(x.MealId))
                .ToList()
        };
    }
}
