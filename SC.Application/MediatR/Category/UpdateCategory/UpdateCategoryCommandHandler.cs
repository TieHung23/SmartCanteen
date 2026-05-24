using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using CategoryAggregateRoot = SC.Domain.Domain.Category.AggregateRoot.Category;

namespace SC.Application.MediatR.Category.UpdateCategory;

internal class UpdateCategoryCommandHandler(
    IRepositoryBase<CategoryAggregateRoot, Guid> categoryRepository,
    ICurrentUserService currentUserService,
    ILogger<UpdateCategoryCommandHandler> logger
) : ICommandHandler<UpdateCategoryCommand, UpdateCategoryResponse>
{
    public async Task<Result<UpdateCategoryResponse>> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryRepository.FindByIdAsync(request.Id, cancellationToken);
            if (category is null || category.IsDeleted)
            {
                return Result.Failure<UpdateCategoryResponse>(Error.NullValue, "Category not found.");
            }

            category.Update(
                request.Name.Trim(),
                request.Description.Trim(),
                currentUserService.UserId);

            var updateResult = await categoryRepository.UpdateAsync(category);
            if (updateResult.IsFailure)
            {
                return Result.Failure<UpdateCategoryResponse>(
                    updateResult.Error ?? Error.ServerError,
                    updateResult.Message);
            }

            var response = new UpdateCategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };

            return Result.Success(response, "Category updated successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating category {CategoryId}", request.Id);
            return Result.Failure<UpdateCategoryResponse>(
                Error.ServerError,
                "An error occurred while updating the category.");
        }
    }
}
