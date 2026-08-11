using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Order.Enum;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Session.GetSessionDishQuantities;

internal sealed class GetSessionDishQuantitiesQueryHandler(
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ILogger<GetSessionDishQuantitiesQueryHandler> logger)
    : IQueryHandler<GetSessionDishQuantitiesQuery, GetSessionDishQuantitiesResponse>
{
    public async Task<Result<GetSessionDishQuantitiesResponse>> Handle(
        GetSessionDishQuantitiesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.SessionId == Guid.Empty)
            {
                return Result.Failure<GetSessionDishQuantitiesResponse>(
                    Error.InvalidValue,
                    "Session id is required.");
            }

            var session = await sessionRepository.FindSingleAsync(
                session => session.Id == request.SessionId && !session.IsDeleted,
                query => query.Include(session => session.SessionDishes),
                cancellationToken);

            if (session is null)
            {
                return Result.Failure<GetSessionDishQuantitiesResponse>(
                    Error.SessionNotFound,
                    "Session was not found.");
            }

            // Cancelled and expired orders no longer consume prepared food, so they do
            // not count toward what the kitchen still owes for this session.
            var orders = await orderRepository.FindListAsync(
                order => order.SessionId == request.SessionId
                         && !order.IsDeleted
                         && order.Status != OrderStatus.Cancelled
                         && order.Status != OrderStatus.Expired,
                query => query.Include(order => order.OrderItems),
                cancellationToken);

            var orderedQuantities = orders
                .SelectMany(order => order.OrderItems)
                .Where(item => item.ItemStatus != OrderItemStatus.Refunded)
                .GroupBy(item => item.DishId)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

            // Report on the session menu, plus any dish that was ordered but has since
            // been taken off the menu — a manager still has to serve those portions.
            var dishIds = session.SessionDishes
                .Select(sessionDish => sessionDish.DishId)
                .Concat(orderedQuantities.Keys)
                .Distinct()
                .ToList();

            List<DishAggregateRoot> dishes = dishIds.Count == 0
                ? []
                : await dishRepository.FindListAsync(
                    dish => dishIds.Contains(dish.Id),
                    cancellationToken);
            var dishMap = dishes.ToDictionary(dish => dish.Id);

            var items = dishIds
                .Select(dishId => new SessionDishQuantityDto
                {
                    DishId = dishId,
                    DishName = dishMap.GetValueOrDefault(dishId)?.Name ?? string.Empty,
                    OrderedQuantity = orderedQuantities.GetValueOrDefault(dishId)
                })
                .OrderByDescending(item => item.OrderedQuantity)
                .ThenBy(item => item.DishName)
                .ToList();

            var response = new GetSessionDishQuantitiesResponse
            {
                SessionId = session.Id,
                SessionName = session.Name,
                TotalOrderedQuantity = items.Sum(item => item.OrderedQuantity),
                Dishes = items
            };

            return Result.Success(response, "Session dish quantities retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving dish quantities for session {SessionId}", request.SessionId);
            return Result.Failure<GetSessionDishQuantitiesResponse>(
                Error.ServerError,
                "An error occurred while retrieving session dish quantities.");
        }
    }
}
