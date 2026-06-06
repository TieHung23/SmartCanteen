using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using MealAggregateRoot = SC.Domain.Domain.Meal.AggregateRoot.Meal;

namespace SC.Application.MediatR.Meal.DeleteMeal;

internal class DeleteMealCommandHandler(
    IGenericRepository<MealAggregateRoot, Guid> mealRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteMealCommandHandler> logger
) : ICommandHandler<DeleteMealCommand, DeleteMealResponse>
{
    public async Task<Result<DeleteMealResponse>> Handle(
        DeleteMealCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var meal = await mealRepository.GetByIdAsync(request.Id, cancellationToken);
            if (meal is null)
            {
                return Result.Failure<DeleteMealResponse>(
                    Error.NullValue,
                    $"Meal with id {request.Id} not found.");
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            meal.SoftDelete();
            mealRepository.Update(meal);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new DeleteMealResponse
            {
                Id = request.Id,
                Message = "Meal deleted successfully (soft delete)."
            };

            return Result.Success(response, "Meal deleted successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error deleting meal with id {MealId}", request.Id);
            return Result.Failure<DeleteMealResponse>(
                Error.ServerError,
                "An error occurred while deleting the meal.");
        }
    }
}
