using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Meal.ValueObject;
using SC.Domain.SharedKernel.ValueObjects;
using MealAggregateRoot = SC.Domain.Domain.Meal.AggregateRoot.Meal;

namespace SC.Application.MediatR.Meal.CreateMeal;

internal class CreateMealCommandHandler(
    IRepositoryBase<MealAggregateRoot, Guid> mealRepository,
    ICurrentUserService currentUserService,
    ILogger<CreateMealCommandHandler> logger
) : ICommandHandler<CreateMealCommand, CreateMealResponse>
{
    public async Task<Result<CreateMealResponse>> Handle(
        CreateMealCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result.Failure<CreateMealResponse>(
                    Error.InvalidValue,
                    "Meal name is required.");
            }

            if (request.PriceAmount <= 0)
            {
                return Result.Failure<CreateMealResponse>(
                    Error.InvalidValue,
                    "Meal price must be greater than zero.");
            }

            var currentUserId = currentUserService.UserId;
            var price = Money.Create(request.PriceAmount, request.PriceCurrency);

            var meal = MealAggregateRoot.Create(
                request.Name,
                request.Description,
                price,
                request.AvailableFrom,
                request.AvailableTo,
                request.AvailableForOrder,
                currentUserId);

            foreach (var setting in request.MealSettings)
            {
                if (setting.Quantity <= 0)
                {
                    return Result.Failure<CreateMealResponse>(
                        Error.InvalidValue,
                        $"Quantity for category {setting.CategoryId} must be greater than zero.");
                }

                var mealSetting = MealSettings.Create(setting.CategoryId, setting.Quantity, meal.Id);
                meal.AddMealCategory(mealSetting);
            }

            var addResult = await mealRepository.AddAsync(meal);
            if (addResult.IsFailure)
            {
                return Result.Failure<CreateMealResponse>(
                    addResult.Error ?? Error.ServerError,
                    addResult.Message);
            }

            var response = new CreateMealResponse
            {
                Id = meal.Id,
                Name = meal.Name,
                Message = "Meal created successfully."
            };

            return Result.Success(response, "Meal created successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating meal");
            return Result.Failure<CreateMealResponse>(
                Error.ServerError,
                "An error occurred while creating the meal.");
        }
    }
}
