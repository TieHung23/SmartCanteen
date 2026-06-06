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
            var meal = await mealRepository.GetByIdAsync(request.Id, cancellationToken);

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
                PriceAmount = meal.Price.Amount,
                IsActive = meal.IsActive,
                AvailableFrom = meal.AvailableFrom,
                AvailableTo = meal.AvailableTo,
                AvailableForOrder = meal.AvailableForOrder,
                MealSettings = meal.MealTemplates
                    .SelectMany(t => t.Settings)
                    .Select(s => new MealSettingDto
                    {
                        CategoryId = s.CategoryId,
                        Quantity = s.MinQuantity
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
