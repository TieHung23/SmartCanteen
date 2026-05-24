using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Meal.ValueObject;
using SC.Domain.SharedKernel.ValueObjects;
using MealAggregateRoot = SC.Domain.Domain.Meal.AggregateRoot.Meal;

namespace SC.Application.MediatR.Meal.UpdateMeal;

internal class UpdateMealCommandHandler(
    IRepositoryBase<MealAggregateRoot, Guid> mealRepository,
    ICurrentUserService currentUserService,
    ILogger<UpdateMealCommandHandler> logger
) : ICommandHandler<UpdateMealCommand, UpdateMealResponse>
{
    public async Task<Result<UpdateMealResponse>> Handle(
        UpdateMealCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var meal = await mealRepository.FindByIdAsync(request.Id, cancellationToken);

            if (meal is null)
            {
                return Result.Failure<UpdateMealResponse>(
                    Error.NullValue,
                    $"Meal with id {request.Id} not found.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result.Failure<UpdateMealResponse>(
                    Error.InvalidValue,
                    "Meal name is required.");
            }

            if (request.PriceAmount <= 0)
            {
                return Result.Failure<UpdateMealResponse>(
                    Error.InvalidValue,
                    "Meal price must be greater than zero.");
            }

            var currentUserId = currentUserService.UserId;
            var price = Money.Create(request.PriceAmount, request.PriceCurrency);

            meal.Update(
                request.Name,
                request.Description,
                price,
                request.AvailableFrom,
                request.AvailableTo,
                request.AvailableForOrder,
                request.IsActive,
                currentUserId);

            // Update meal settings
            meal.ClearMealCategories();
            foreach (var setting in request.MealSettings)
            {
                if (setting.Quantity <= 0)
                {
                    return Result.Failure<UpdateMealResponse>(
                        Error.InvalidValue,
                        $"Quantity for category {setting.CategoryId} must be greater than zero.");
                }

                var mealSetting = MealSettings.Create(setting.CategoryId, setting.Quantity, meal.Id);
                meal.AddMealCategory(mealSetting);
            }

            var updateResult = await mealRepository.UpdateAsync(meal);
            if (updateResult.IsFailure)
            {
                return Result.Failure<UpdateMealResponse>(
                    updateResult.Error ?? Error.ServerError,
                    updateResult.Message);
            }

            var response = new UpdateMealResponse
            {
                Id = meal.Id,
                Name = meal.Name,
                Message = "Meal updated successfully."
            };

            return Result.Success(response, "Meal updated successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating meal with id {MealId}", request.Id);
            return Result.Failure<UpdateMealResponse>(
                Error.ServerError,
                "An error occurred while updating the meal.");
        }
    }
}
