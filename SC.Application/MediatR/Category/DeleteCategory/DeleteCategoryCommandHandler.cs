using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using CategoryAggregateRoot = SC.Domain.Domain.Category.AggregateRoot.Category;

namespace SC.Application.MediatR.Category.DeleteCategory;

internal class DeleteCategoryCommandHandler(
    IRepositoryBase<CategoryAggregateRoot, Guid> categoryRepository,
    ICurrentUserService currentUserService,
    ILogger<DeleteCategoryCommandHandler> logger
) : ICommandHandler<DeleteCategoryCommand, DeleteCategoryResponse>
{
    public async Task<Result<DeleteCategoryResponse>> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryRepository.FindByIdAsync(request.Id, cancellationToken);
            if (category is null || category.IsDeleted)
            {
                return Result.Failure<DeleteCategoryResponse>(
                    Error.NullValue,
                    "Category not found.");
            }

            category.SoftDelete(currentUserService.UserId);

            var deleteResult = await categoryRepository.UpdateAsync(category);
            if (deleteResult.IsFailure)
            {
                return Result.Failure<DeleteCategoryResponse>(
                    deleteResult.Error ?? Error.ServerError,
                    deleteResult.Message);
            }

            var response = new DeleteCategoryResponse
            {
                Id = request.Id,
                Message = "Category deleted successfully."
            };

            return Result.Success(response, response.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting category {CategoryId}", request.Id);
            return Result.Failure<DeleteCategoryResponse>(
                Error.ServerError,
                "An error occurred while deleting the category.");
        }
    }
}
