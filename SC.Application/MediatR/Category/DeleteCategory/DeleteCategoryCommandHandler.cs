using Microsoft.Extensions.Logging;
using SC.Application.MediatR.Session;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using CategoryAggregateRoot = SC.Domain.Domain.Category.AggregateRoot.Category;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Category.DeleteCategory;

internal class DeleteCategoryCommandHandler(
    IGenericRepository<CategoryAggregateRoot, Guid> categoryRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<DeleteCategoryCommandHandler> logger
) : ICommandHandler<DeleteCategoryCommand, DeleteCategoryResponse>
{
    public async Task<Result<DeleteCategoryResponse>> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken);
            if (category is null || category.IsDeleted)
            {
                return Result.Failure<DeleteCategoryResponse>(
                    Error.NullValue,
                    "Category not found.");
            }

            // Deleting the category would orphan any dish still bound to it, so the
            // dishes have to be deactivated or moved elsewhere first.
            var activeDishes = await dishRepository.FindListAsync(
                dish => dish.CategoryId == request.Id && !dish.IsDeleted && dish.IsActive,
                cancellationToken);
            if (activeDishes.Count > 0)
            {
                return Result.Failure<DeleteCategoryResponse>(
                    Error.ResourceBusy,
                    "Cannot delete a category that still has active dishes.",
                    $"Active dishes: {string.Join(", ", activeDishes.Select(dish => dish.Name))}.");
            }

            var blockingSessions = await CurrentSessionUsage.FindSessionNamesUsingCategoryAsync(
                sessionRepository, dishRepository, request.Id, cancellationToken);
            if (blockingSessions.Count > 0)
            {
                return Result.Failure<DeleteCategoryResponse>(
                    Error.ResourceBusy,
                    "Cannot delete a category that is used by a current session.",
                    CurrentSessionUsage.Describe(blockingSessions));
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            category.SoftDelete();
            categoryRepository.Update(category);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new DeleteCategoryResponse
            {
                Id = request.Id,
                Message = "Category deleted successfully."
            };

            return Result.Success(response, response.Message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error deleting category {CategoryId}", request.Id);
            return Result.Failure<DeleteCategoryResponse>(
                Error.ServerError,
                "An error occurred while deleting the category.");
        }
    }
}
