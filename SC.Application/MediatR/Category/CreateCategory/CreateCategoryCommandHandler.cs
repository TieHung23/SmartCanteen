using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using CategoryAggregateRoot = SC.Domain.Domain.Category.AggregateRoot.Category;

namespace SC.Application.MediatR.Category.CreateCategory;

internal class CreateCategoryCommandHandler(
    IGenericRepository<CategoryAggregateRoot, Guid> categoryRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
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

            category.ImgUrl = request.ImgUrl;

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await categoryRepository.AddAsync(category, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new CreateCategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImgUrl = category.ImgUrl
            };

            return Result.Success(response, "Category created successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error creating category");
            return Result.Failure<CreateCategoryResponse>(
                Error.ServerError,
                "An error occurred while creating the category.");
        }
    }
}
