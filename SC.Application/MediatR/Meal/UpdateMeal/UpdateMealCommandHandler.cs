using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Dish.AggregateRoot;
using SC.Domain.Domain.Meal.Entity;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using MealAggregateRoot = SC.Domain.Domain.Meal.AggregateRoot.Meal;

namespace SC.Application.MediatR.Meal.UpdateMeal;

internal class UpdateMealCommandHandler(
    IGenericRepository<MealAggregateRoot, Guid> mealRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<UpdateMealCommandHandler> logger
) : ICommandHandler<UpdateMealCommand, UpdateMealResponse>
{
    public async Task<Result<UpdateMealResponse>> Handle(
        UpdateMealCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var meal = await mealRepository.GetByIdAsync(request.Id, cancellationToken);

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

            var currentUserId = currentUserService.UserId;

            meal.Update(
                request.Name,
                request.Description,
                request.AvailableFrom,
                request.AvailableTo,
                request.AvailableForOrder,
                request.IsActive,
                currentUserId);

            // Update meal templates
            meal.ClearMealTemplates();
            foreach (var templateInput in request.MealTemplates)
            {
                if (string.IsNullOrWhiteSpace(templateInput.Name))
                {
                    return Result.Failure<UpdateMealResponse>(
                        Error.InvalidValue,
                        "Template name is required.");
                }

                var template = MealTemplate.Create(meal.Id, templateInput.Name);

                foreach (var setting in templateInput.Settings)
                {
                    if (setting.MinQuantity < 0)
                    {
                        return Result.Failure<UpdateMealResponse>(
                            Error.InvalidValue,
                            $"MinQuantity for category {setting.CategoryId} cannot be negative.");
                    }

                    if (setting.MaxQuantity < setting.MinQuantity)
                    {
                        return Result.Failure<UpdateMealResponse>(
                            Error.InvalidValue,
                            $"MaxQuantity for category {setting.CategoryId} must be >= MinQuantity.");
                    }

                    template.AddSetting(setting.CategoryId, setting.MinQuantity, setting.MaxQuantity, setting.IsRequired);
                }

                meal.AddMealTemplate(template);
            }

            // Update dish meals
            meal.DishMeals.Clear();
            foreach (var dishInput in request.Dishes)
            {
                var dish = await dishRepository.GetByIdAsync(dishInput.DishId, cancellationToken);
                if (dish is null || dish.IsDeleted || !dish.IsActive)
                {
                    return Result.Failure<UpdateMealResponse>(
                        Error.NullValue,
                        $"Dish with id {dishInput.DishId} not found or inactive.");
                }

                meal.AddDishMeal(new DishMeal
                {
                    DishId = dishInput.DishId,
                    MealId = meal.Id,
                    Quantity = dishInput.Quantity > 0 ? dishInput.Quantity : 1
                });
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            mealRepository.Update(meal);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

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
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error updating meal with id {MealId}", request.Id);
            return Result.Failure<UpdateMealResponse>(
                Error.ServerError,
                "An error occurred while updating the meal.");
        }
    }
}
