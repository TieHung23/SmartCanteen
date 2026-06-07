using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Dish.AggregateRoot;
using SC.Domain.SharedKernel.ValueObjects;
using CategoryAggregateRoot = SC.Domain.Domain.Category.AggregateRoot.Category;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Dish.CreateDish;

internal class CreateDishCommandHandler(
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<CategoryAggregateRoot, Guid> categoryRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<CreateDishCommandHandler> logger
) : ICommandHandler<CreateDishCommand, CreateDishResponse>
{
    public async Task<Result<CreateDishResponse>> Handle(
        CreateDishCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category is null || category.IsDeleted)
            {
                return Result.Failure<CreateDishResponse>(Error.NullValue, "Category not found.");
            }

            var dish = DishAggregateRoot.Create(
                request.Name.Trim(),
                request.Description.Trim(),
                Money.Create(request.Price),
                request.CategoryId,
                currentUserService.UserId,
                request.ImgUrl);

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await dishRepository.AddAsync(dish, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new CreateDishResponse
            {
                Id = dish.Id,
                Name = dish.Name,
                Description = dish.Description,
                Price = dish.Price.Amount,
                IsActive = dish.IsActive,
                CategoryId = dish.CategoryId,
                ImgUrl = dish.ImgUrl
            };

            return Result.Success(response, "Dish created successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error creating dish");
            return Result.Failure<CreateDishResponse>(
                Error.ServerError,
                "An error occurred while creating dish.");
        }
    }
}
