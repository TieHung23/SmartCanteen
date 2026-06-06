using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Dish.GetAllDishes;

internal class GetAllDishesQueryHandler(
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ILogger<GetAllDishesQueryHandler> logger
) : IQueryHandler<GetAllDishesQuery, PaginatedList<GetAllDishesResponse>>
{
    public async Task<Result<PaginatedList<GetAllDishesResponse>>> Handle(
        GetAllDishesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            IQueryable<DishAggregateRoot> query = dishRepository.GetQueryable(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var normalizedName = request.Name.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(normalizedName));
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == request.CategoryId.Value);
            }

            if (request.MealId.HasValue)
            {
                query = query.Where(x => x.DishMeals.Any(dm => dm.MealId == request.MealId.Value));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var skipCount = request.GetSkipCount();
            var paginatedDishes = await query
                .Include(x => x.DishMeals)
                .OrderBy(x => x.Name)
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var responses = paginatedDishes.Select(d => new GetAllDishesResponse
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                Price = d.Price.Amount,
                IsActive = d.IsActive,
                MealIds = d.DishMeals.Select(dm => dm.MealId).ToList(),
                CategoryId = d.CategoryId
            }).ToList();

            var paginatedResult = new PaginatedList<GetAllDishesResponse>(
                responses,
                request.PageNumber,
                request.PageSize,
                totalCount);

            return Result.Success(paginatedResult, "Dishes retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving dishes");
            return Result.Failure<PaginatedList<GetAllDishesResponse>>(
                Error.ServerError,
                "An error occurred while retrieving dishes.");
        }
    }
}
