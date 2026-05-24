using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.SharedKernel.ValueObjects;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using UserAggregateRoot = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Order.CreateOrder;

internal class CreateOrderCommandHandler(
    IRepositoryBase<OrderAggregateRoot, Guid> orderRepository,
    IRepositoryBase<UserAggregateRoot, Guid> userRepository,
    ICurrentUserService currentUserService,
    ILogger<CreateOrderCommandHandler> logger
) : ICommandHandler<CreateOrderCommand, CreateOrderResponse>
{
    public async Task<Result<CreateOrderResponse>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!request.Items.Any())
            {
                return Result.Failure<CreateOrderResponse>(
                    Error.InvalidValue,
                    "Order must contain at least one item.");
            }

            if (request.Items.Any(item => item.Quantity <= 0))
            {
                return Result.Failure<CreateOrderResponse>(
                    Error.InvalidValue,
                    "Item quantity must be greater than zero.");
            }

            if (request.Items.Any(item => item.UnitPrice <= 0))
            {
                return Result.Failure<CreateOrderResponse>(
                    Error.InvalidValue,
                    "Item price must be greater than zero.");
            }

            var currentUserId = currentUserService.UserId;
            var user = await userRepository.FindByIdAsync(currentUserId, cancellationToken);

            if (user is null)
            {
                return Result.Failure<CreateOrderResponse>(
                    Error.NullValue,
                    "User not found.");
            }

            // Calculate total order price
            var totalPrice = request.Items.Sum(item => item.UnitPrice * item.Quantity);
            var currency = "VND";

            // Check if user has sufficient balance
            if (user.Balance.Amount < totalPrice)
            {
                return Result.Failure<CreateOrderResponse>(
                    Error.InvalidValue,
                    $"Insufficient balance. Required: {totalPrice}, Available: {user.Balance.Amount}");
            }

            // Create the order
            var order = OrderAggregateRoot.Create(request.MealId, currentUserId);

            // Add items to order
            foreach (var item in request.Items)
            {
                order.AddDish(item.DishId, item.Quantity, item.UnitPrice, currency);
            }

            // Deduct from user wallet
            var newBalance = Money.Create(user.Balance.Amount - totalPrice, currency);
            user.Balance = newBalance;

            // Save changes
            var userUpdateResult = await userRepository.UpdateAsync(user);
            if (userUpdateResult.IsFailure)
            {
                return Result.Failure<CreateOrderResponse>(
                    userUpdateResult.Error ?? Error.ServerError,
                    userUpdateResult.Message);
            }

            var orderAddResult = await orderRepository.AddAsync(order);
            if (orderAddResult.IsFailure)
            {
                return Result.Failure<CreateOrderResponse>(
                    orderAddResult.Error ?? Error.ServerError,
                    orderAddResult.Message);
            }

            var response = new CreateOrderResponse
            {
                Id = order.Id,
                TotalPrice = totalPrice,
                Currency = currency,
                Message = "Order created successfully and wallet debited.",
                UserRemainingBalance = newBalance.Amount
            };

            return Result.Success(response, "Order created successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating order for user {UserId}", currentUserService.UserId);
            return Result.Failure<CreateOrderResponse>(
                Error.ServerError,
                "An error occurred while creating the order.");
        }
    }
}
