using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.WalletTransaction.Entity;
using SC.Domain.Domain.WalletTransaction.Enum;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Application.MediatR.Order.CreateOrder;

internal class CreateOrderCommandHandler(
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IUserRepository userRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<WalletTransaction, Guid> walletTransactionRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
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

            var currentUserId = currentUserService.UserId;
            var dishIds = request.Items.Select(item => item.DishId).Distinct().ToList();
            var dishes = dishRepository.GetQueryable(dish => dishIds.Contains(dish.Id))
                .ToList()
                .Where(dish => dish is not null)
                .ToDictionary(dish => dish!.Id, dish => dish!);

            if (dishes.Count != dishIds.Count)
            {
                return Result.Failure<CreateOrderResponse>(
                    Error.NullValue,
                    "One or more dishes were not found.");
            }

            if (dishes.Values.Any(dish => dish.IsDeleted || !dish.IsActive))
            {
                return Result.Failure<CreateOrderResponse>(
                    Error.InvalidValue,
                    "One or more dishes are not available.");
            }

            var totalPrice = request.Items.Sum(item =>
            {
                var dish = dishes[item.DishId];
                return dish.Price.Amount * item.Quantity;
            });

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await unitOfWork.LockUserAsync(currentUserId, cancellationToken);

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

            if (user.Balance.Amount < totalPrice)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<CreateOrderResponse>(
                    Error.InvalidValue,
                    $"Insufficient balance. Required: {totalPrice}, Available: {user.Balance.Amount}");
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

            var order = OrderAggregateRoot.Create(request.MealId, currentUserId);

            foreach (var item in request.Items)
            {
                var dish = dishes[item.DishId];
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

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new CreateOrderResponse
            {
                Id = order.Id,
                TransactionId = transaction.Id,
                TotalPrice = totalPrice,
                Message = "Order created successfully and wallet debited.",
                UserRemainingBalance = balanceAfter.Value
            };

            return Result.Success(response, "Order created successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error creating order for user {UserId}", currentUserService.UserId);
            return Result.Failure<CreateOrderResponse>(
                Error.ServerError,
                "An error occurred while creating the order.");
        }
    }
}
