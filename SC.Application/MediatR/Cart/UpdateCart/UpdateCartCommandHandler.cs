using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using CartAggregateRoot = SC.Domain.Domain.Cart.AggregateRoot.Cart;

namespace SC.Application.MediatR.Cart.UpdateCart;

internal sealed class UpdateCartCommandHandler(
    IGenericRepository<CartAggregateRoot, Guid> cartRepository,
    ICartValidationService cartValidationService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<UpdateCartCommandHandler> logger)
    : ICommandHandler<UpdateCartCommand, CartResponse>
{
    public async Task<Result<CartResponse>> Handle(
        UpdateCartCommand request,
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
            var validationResult = await cartValidationService.ValidateAsync(
                request.Data,
                cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<CartResponse>(
                    validationResult.Error!,
                    validationResult.Message);
            }

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

            var dataJson = CartJson.Serialize(request.Data!);
            if (cart is null)
            {
                cart = CartAggregateRoot.Create(userId, dataJson);
                await cartRepository.AddAsync(cart, cancellationToken);
            }
            else
            {
                cart.Update(dataJson, userId);
                cartRepository.Update(cart);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(
                new CartResponse
                {
                    Id = cart.Id,
                    Data = CartJson.Deserialize(cart.DataJson),
                    Version = cart.Version,
                    UpdatedAtUtc = cart.UpdatedAtUtc ?? cart.CreatedAtUtc
                },
                "Cart updated successfully.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogWarning(ex, "Cart version conflict for user {UserId}", userId);
            return Result.Failure<CartResponse>(
                Error.CartVersionConflict,
                "The cart was updated by another client. Reload the cart and try again.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error updating cart for user {UserId}", userId);
            return Result.Failure<CartResponse>(
                Error.ServerError,
                "An error occurred while updating the cart.");
        }
    }
}
