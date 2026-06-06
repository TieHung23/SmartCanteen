using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using CategoryAggregateRoot = SC.Domain.Domain.Category.AggregateRoot.Category;

namespace SC.Application.MediatR.Category.GetCategoryById;

internal class GetCategoryByIdQueryHandler(
    IGenericRepository<CategoryAggregateRoot, Guid> categoryRepository,
    ILogger<GetCategoryByIdQueryHandler> logger
) : IQueryHandler<GetCategoryByIdQuery, GetCategoryByIdResponse>
{
    public async Task<Result<GetCategoryByIdResponse>> Handle(
        GetCategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken);
            if (category is null || category.IsDeleted)
            {
                return Result.Failure<GetCategoryByIdResponse>(Error.NullValue, "Category not found.");
            }

            var response = new GetCategoryByIdResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                CreatedAtUtc = category.CreatedAtUtc,
                UpdatedAtUtc = category.UpdatedAtUtc,
                CreatedBy = category.CreatedBy,
                UpdatedBy = category.UpdatedBy
            };

            return Result.Success(response, "Category retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving category {CategoryId}", request.Id);
            return Result.Failure<GetCategoryByIdResponse>(
                Error.ServerError,
                "An error occurred while retrieving the category.");
        }
    }
}
