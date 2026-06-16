using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Application.MediatR.Cart;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Contract.Services.Notification;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.WalletTransaction.Entity;
using SC.Domain.Domain.WalletTransaction.Enum;
using CartAggregateRoot = SC.Domain.Domain.Cart.AggregateRoot.Cart;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Application.MediatR.Order.CreateOrder;

internal class CreateOrderCommandHandler(
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IUserRepository userRepository,
    IGenericRepository<CartAggregateRoot, Guid> cartRepository,
    IGenericRepository<WalletTransaction, Guid> walletTransactionRepository,
    ICartValidationService cartValidationService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IBusinessNotificationService businessNotificationService,
    ILogger<CreateOrderCommandHandler> logger
) : ICommandHandler<CreateOrderCommand, CreateOrderResponse>
{
    public async Task<Result<CreateOrderResponse>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;

        try
        {
            if (request.CartVersion <= 0)
            {
                return Result.Failure<CreateOrderResponse>(
                    Error.InvalidValue,
                    "CartVersion must be greater than zero.");
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await unitOfWork.LockUserAsync(currentUserId, cancellationToken);

            var cart = await cartRepository
                .GetQueryable(x => x.UserId == currentUserId)
                .SingleOrDefaultAsync(cancellationToken);

            if (cart is null)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<CreateOrderResponse>(
                    Error.NullValue,
                    "Cart was not found.");
            }

            if (cart.Version != request.CartVersion)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<CreateOrderResponse>(
                    Error.CartVersionConflict,
                    $"Cart version conflict. Current version is {cart.Version}.");
            }

            CartData cartData;
            try
            {
                cartData = CartJson.Deserialize(cart.DataJson);
            }
            catch (System.Text.Json.JsonException)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<CreateOrderResponse>(
                    Error.InvalidValue,
                    "Stored cart data is invalid.");
            }

            var validationResult = await cartValidationService.ValidateAsync(
                cartData,
                cancellationToken);
            if (validationResult.IsFailure)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<CreateOrderResponse>(
                    validationResult.Error!,
                    validationResult.Message);
            }

            var user = await userRepository.GetByIdAsync(
                currentUserId,
                cancellationToken);

            if (user is null)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<CreateOrderResponse>(
                    Error.NullValue,
                    "User not found.");
            }

            var validatedCart = validationResult.Value!;
            var items = cartData.Items!;
            var totalPrice = items.Sum(item =>
                validatedCart.Dishes[item.DishId].Price.Amount * item.Quantity);

            if (user.Balance.Amount < totalPrice)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<CreateOrderResponse>(
                    Error.InvalidValue,
                    $"Insufficient balance. Required: {totalPrice}, Available: {user.Balance.Amount}");
            }

            foreach (var item in items.OrderBy(x => x.DishId))
            {
                var reserved = await unitOfWork.TryReserveMealDishAsync(
                    cartData.MealId,
                    item.DishId,
                    item.Quantity,
                    cancellationToken);

                if (!reserved)
                {
                    await unitOfWork.RollbackAsync(cancellationToken);
                    return Result.Failure<CreateOrderResponse>(
                        Error.InsufficientDishStock,
                        "One or more dishes ran out of stock. Reload the cart and try again.");
                }
            }

            var balanceAfter = await unitOfWork.TryDebitUserBalanceAsync(
                currentUserId,
                totalPrice,
                cancellationToken);

            if (balanceAfter is null)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<CreateOrderResponse>(
                    Error.InvalidValue,
                    "Insufficient balance.");
            }

            var order = OrderAggregateRoot.Create(
                cartData.MealId,
                cartData.MealTemplateId,
                currentUserId);

            foreach (var item in items)
            {
                var dish = validatedCart.Dishes[item.DishId];
                order.AddDish(item.DishId, item.Quantity, dish.Price.Amount);
            }

            var balanceBefore = balanceAfter.Value + totalPrice;

            var transaction = WalletTransaction.Create(
                currentUserId,
                -totalPrice,
                balanceBefore,
                balanceAfter.Value,
                WalletTransactionType.OrderPayment);

            await walletTransactionRepository.AddAsync(transaction, cancellationToken);
            order.AttachTransaction(transaction.Id, currentUserId);
            await orderRepository.AddAsync(order, cancellationToken);

            cart.Update(CartJson.Serialize(new CartData()), currentUserId);
            cartRepository.Update(cart);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new CreateOrderResponse
            {
                Id = order.Id,
                TransactionId = transaction.Id,
                TotalPrice = totalPrice,
                Message = "Order created successfully and wallet debited.",
                UserRemainingBalance = balanceAfter.Value,
                CartVersion = cart.Version
            };

            await businessNotificationService.NotifyAsync(
                NotificationTemplateKeys.OrderCreated,
                currentUserId,
                order.Id,
                new Dictionary<string, string>
                {
                    ["referenceId"] = order.Id.ToString(),
                    ["totalPrice"] = totalPrice.ToString("0.##")
                },
                new
                {
                    OrderId = order.Id,
                    TransactionId = transaction.Id,
                    TotalPrice = totalPrice
                },
                cancellationToken);

            return Result.Success(response, "Order created successfully.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogWarning(ex, "Cart version conflict while creating order for user {UserId}", currentUserId);
            return Result.Failure<CreateOrderResponse>(
                Error.CartVersionConflict,
                "The cart was updated by another client. Reload the cart and try again.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error creating order for user {UserId}", currentUserId);
            return Result.Failure<CreateOrderResponse>(
                Error.ServerError,
                "An error occurred while creating the order.");
        }
    }
}
