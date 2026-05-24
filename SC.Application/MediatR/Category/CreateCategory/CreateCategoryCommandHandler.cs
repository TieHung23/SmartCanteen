using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using CategoryAggregateRoot = SC.Domain.Domain.Category.AggregateRoot.Category;

namespace SC.Application.MediatR.Category.CreateCategory;

internal class CreateCategoryCommandHandler(
    IRepositoryBase<CategoryAggregateRoot, Guid> categoryRepository,
    ICurrentUserService currentUserService,
    ILogger<CreateCategoryCommandHandler> logger
) : ICommandHandler<CreateCategoryCommand, CreateCategoryResponse>
{
    public async Task<Result<CreateCategoryResponse>> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = CategoryAggregateRoot.Create(
                request.Name.Trim(),
                request.Description.Trim(),
                currentUserService.UserId);

            var createResult = await categoryRepository.AddAsync(category);
            if (createResult.IsFailure)
            {
                return Result.Failure<CreateCategoryResponse>(
                    createResult.Error ?? Error.ServerError,
                    createResult.Message);
            }

            var response = new CreateCategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };

            return Result.Success(response, "Category created successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating category");
            return Result.Failure<CreateCategoryResponse>(
                Error.ServerError,
                "An error occurred while creating the category.");
        }
    }
}
