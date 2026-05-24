using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using MealAggregateRoot = SC.Domain.Domain.Meal.AggregateRoot.Meal;

namespace SC.Application.MediatR.Meal.DeleteMeal;

internal class DeleteMealCommandHandler(
    IRepositoryBase<MealAggregateRoot, Guid> mealRepository,
    ILogger<DeleteMealCommandHandler> logger
) : ICommandHandler<DeleteMealCommand, DeleteMealResponse>
{
    public async Task<Result<DeleteMealResponse>> Handle(
        DeleteMealCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Soft delete meal by ID
            var deleteResult = await mealRepository.SoftDeleteWithConditionAsync(
                m => m.Id == request.Id);

            if (deleteResult.IsFailure)
            {
                return Result.Failure<DeleteMealResponse>(
                    deleteResult.Error ?? Error.ServerError,
                    deleteResult.Message);
            }

            // Check if entity was found (SoftDeleteWithConditionAsync returns success even if count is 0)
            var meal = await mealRepository.FindByIdAsync(request.Id, cancellationToken);
            if (meal is null)
            {
                return Result.Failure<DeleteMealResponse>(
                    Error.NullValue,
                    $"Meal with id {request.Id} not found.");
            }

            var response = new DeleteMealResponse
            {
                Id = request.Id,
                Message = "Meal deleted successfully (soft delete)."
            };

            return Result.Success(response, "Meal deleted successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting meal with id {MealId}", request.Id);
            return Result.Failure<DeleteMealResponse>(
                Error.ServerError,
                "An error occurred while deleting the meal.");
        }
    }
}
