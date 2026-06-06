using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using MealAggregateRoot = SC.Domain.Domain.Meal.AggregateRoot.Meal;

namespace SC.Application.MediatR.Meal.GetAllMeals;

internal class GetAllMealsQueryHandler(
    IGenericRepository<MealAggregateRoot, Guid> mealRepository,
    ILogger<GetAllMealsQueryHandler> logger
) : IQueryHandler<GetAllMealsQuery, PaginatedList<GetAllMealsResponse>>
{
    public async Task<Result<PaginatedList<GetAllMealsResponse>>> Handle(
        GetAllMealsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            IQueryable<MealAggregateRoot> query = mealRepository.GetQueryable();

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                query = query.Where(x =>
                    x.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var skipCount = request.GetSkipCount();
            var paginatedMeals = await query
                .Include(x => x.DishMeals)
                .OrderBy(x => x.Name)
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var responses = paginatedMeals.Select(m => new GetAllMealsResponse
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                IsActive = m.IsActive,
                AvailableFrom = m.AvailableFrom,
                AvailableTo = m.AvailableTo,
                AvailableForOrder = m.AvailableForOrder,
                Dishes = m.DishMeals
                    .Select(dm => new DishMealDto
                    {
                        DishId = dm.DishId,
                        Quantity = dm.Quantity
                    })
                    .ToList()
            }).ToList();

            var paginatedResult = new PaginatedList<GetAllMealsResponse>(
                responses,
                request.PageNumber,
                request.PageSize,
                totalCount);

            return Result.Success(paginatedResult, "Meals retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving meals");
            return Result.Failure<PaginatedList<GetAllMealsResponse>>(
                Error.ServerError,
                "An error occurred while retrieving meals.");
        }
    }
}
