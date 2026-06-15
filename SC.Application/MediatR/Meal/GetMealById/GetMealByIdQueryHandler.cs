using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using MealAggregateRoot = SC.Domain.Domain.Meal.AggregateRoot.Meal;

namespace SC.Application.MediatR.Meal.GetMealById;

internal class GetMealByIdQueryHandler(
    IGenericRepository<MealAggregateRoot, Guid> mealRepository,
    ILogger<GetMealByIdQueryHandler> logger
) : IQueryHandler<GetMealByIdQuery, GetMealByIdResponse>
{
    public async Task<Result<GetMealByIdResponse>> Handle(
        GetMealByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var meal = await mealRepository.GetQueryable(x => x.Id == request.Id)
                .Include(x => x.MealTemplates)
                    .ThenInclude(x => x.Settings)
                .Include(x => x.DishMeals)
                .FirstOrDefaultAsync(cancellationToken);

            if (meal is null)
            {
                return Result.Failure<GetMealByIdResponse>(
                    Error.NullValue,
                    $"Meal with id {request.Id} not found.");
            }

            var response = new GetMealByIdResponse
            {
                Id = meal.Id,
                Name = meal.Name,
                Description = meal.Description,
                IsActive = meal.IsActive,
                AvailableFrom = meal.AvailableFrom,
                AvailableTo = meal.AvailableTo,
                AvailableForOrder = meal.AvailableForOrder,
                MealTemplates = meal.MealTemplates
                    .Where(t => !t.IsDeleted)
                    .Select(t => new MealTemplateDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Settings = t.Settings
                            .Where(s => !s.IsDeleted)
                            .Select(s => new MealSettingDto
                            {
                                CategoryId = s.CategoryId,
                                MinQuantity = s.MinQuantity,
                                MaxQuantity = s.MaxQuantity,
                                IsRequired = s.IsRequired
                            }).ToList()
                    })
                    .ToList(),
                Dishes = meal.DishMeals
                    .Select(dm => new DishMealDto
                    {
                        DishId = dm.DishId,
                        Quantity = dm.Quantity
                    })
                    .ToList()
            };

            return Result.Success(response, "Meal retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving meal with id {MealId}", request.Id);
            return Result.Failure<GetMealByIdResponse>(
                Error.ServerError,
                "An error occurred while retrieving the meal.");
        }
    }
}
