using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using CartAggregateRoot = SC.Domain.Domain.Cart.AggregateRoot.Cart;

namespace SC.Application.MediatR.Cart.ClearCart;

internal sealed class ClearCartCommandHandler(
    IGenericRepository<CartAggregateRoot, Guid> cartRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<ClearCartCommandHandler> logger)
    : ICommandHandler<ClearCartCommand, CartResponse>
{
    public async Task<Result<CartResponse>> Handle(
        ClearCartCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ExpectedVersion < 0)
        {
            return Result.Failure<CartResponse>(
                Error.InvalidValue,
                "Expected version cannot be negative.");
        }

        var userId = currentUserService.UserId;

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await unitOfWork.LockUserAsync(userId, cancellationToken);

            var cart = await cartRepository
                .GetQueryable(x => x.UserId == userId)
                .SingleOrDefaultAsync(cancellationToken);

            var currentVersion = cart?.Version ?? 0;
            if (currentVersion != request.ExpectedVersion)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<CartResponse>(
                    Error.CartVersionConflict,
                    $"Cart version conflict. Current version is {currentVersion}.");
            }

            if (cart is null)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Success(
                    new CartResponse
                    {
                        Data = new CartData(),
                        Version = 0
                    },
                    "Cart is already empty.");
            }

            cart.Update(CartJson.Serialize(new CartData()), userId);
            cartRepository.Update(cart);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(
                new CartResponse
                {
                    Id = cart.Id,
                    Data = CartJson.Deserialize(cart.DataJson),
                    Version = cart.Version,
                    UpdatedAtUtc = cart.UpdatedAtUtc
                },
                "Cart cleared successfully.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogWarning(ex, "Cart version conflict while clearing cart for user {UserId}", userId);
            return Result.Failure<CartResponse>(
                Error.CartVersionConflict,
                "The cart was updated by another client. Reload the cart and try again.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
            return Result.Failure<CartResponse>(
                Error.ServerError,
                "An error occurred while clearing the cart.");
        }
    }
}
