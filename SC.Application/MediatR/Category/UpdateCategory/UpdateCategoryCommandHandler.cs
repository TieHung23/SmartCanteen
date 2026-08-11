using Microsoft.Extensions.Logging;
using SC.Application.MediatR.Session;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using CategoryAggregateRoot = SC.Domain.Domain.Category.AggregateRoot.Category;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Category.UpdateCategory;

internal class UpdateCategoryCommandHandler(
    IGenericRepository<CategoryAggregateRoot, Guid> categoryRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<UpdateCategoryCommandHandler> logger
) : ICommandHandler<UpdateCategoryCommand, UpdateCategoryResponse>
{
    public async Task<Result<UpdateCategoryResponse>> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken);
            if (category is null || category.IsDeleted)
            {
                return Result.Failure<UpdateCategoryResponse>(Error.NullValue, "Category not found.");
            }

            var blockingSessions = await CurrentSessionUsage.FindSessionNamesUsingCategoryAsync(
                sessionRepository, dishRepository, request.Id, cancellationToken);
            if (blockingSessions.Count > 0)
            {
                return Result.Failure<UpdateCategoryResponse>(
                    Error.ResourceBusy,
                    "Cannot update a category that is used by a current session.",
                    CurrentSessionUsage.Describe(blockingSessions));
            }

            category.Update(
                request.Name.Trim(),
                string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                currentUserService.UserId,
                request.ImgUrl ?? category.ImgUrl);

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            categoryRepository.Update(category);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new UpdateCategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description ?? string.Empty,
                ImgUrl = category.ImgUrl
            };

            return Result.Success(response, "Category updated successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error updating category {CategoryId}", request.Id);
            return Result.Failure<UpdateCategoryResponse>(
                Error.ServerError,
                "An error occurred while updating the category.");
        }
    }
}
