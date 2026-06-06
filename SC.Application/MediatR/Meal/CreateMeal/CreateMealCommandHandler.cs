using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Dish.AggregateRoot;
using SC.Domain.Domain.Meal.Entity;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using MealAggregateRoot = SC.Domain.Domain.Meal.AggregateRoot.Meal;

namespace SC.Application.MediatR.Meal.CreateMeal;

internal class CreateMealCommandHandler(
    IGenericRepository<MealAggregateRoot, Guid> mealRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
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

            var currentUserId = currentUserService.UserId;

            var meal = MealAggregateRoot.Create(
                request.Name,
                request.Description,
                request.AvailableFrom,
                request.AvailableTo,
                request.AvailableForOrder,
                currentUserId);

            foreach (var templateInput in request.MealTemplates)
            {
                if (string.IsNullOrWhiteSpace(templateInput.Name))
                {
                    return Result.Failure<CreateMealResponse>(
                        Error.InvalidValue,
                        "Template name is required.");
                }

                var template = MealTemplate.Create(meal.Id, templateInput.Name);

                foreach (var setting in templateInput.Settings)
                {
                    if (setting.MinQuantity < 0)
                    {
                        return Result.Failure<CreateMealResponse>(
                            Error.InvalidValue,
                            $"MinQuantity for category {setting.CategoryId} cannot be negative.");
                    }

                    if (setting.MaxQuantity < setting.MinQuantity)
                    {
                        return Result.Failure<CreateMealResponse>(
                            Error.InvalidValue,
                            $"MaxQuantity for category {setting.CategoryId} must be >= MinQuantity.");
                    }

                    template.AddSetting(setting.CategoryId, setting.MinQuantity, setting.MaxQuantity, setting.IsRequired);
                }

                meal.AddMealTemplate(template);
            }

            foreach (var dishInput in request.Dishes)
            {
                var dish = await dishRepository.GetByIdAsync(dishInput.DishId, cancellationToken);
                if (dish is null || dish.IsDeleted || !dish.IsActive)
                {
                    return Result.Failure<CreateMealResponse>(
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
            await mealRepository.AddAsync(meal, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

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
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error creating meal");
            return Result.Failure<CreateMealResponse>(
                Error.ServerError,
                "An error occurred while creating the meal.");
        }
    }
}
