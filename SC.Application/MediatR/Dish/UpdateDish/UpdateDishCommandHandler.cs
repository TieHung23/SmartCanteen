using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Dish.AggregateRoot;
using SC.Domain.SharedKernel.ValueObjects;
using CategoryAggregateRoot = SC.Domain.Domain.Category.AggregateRoot.Category;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Dish.UpdateDish;

internal class UpdateDishCommandHandler(
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<CategoryAggregateRoot, Guid> categoryRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<UpdateDishCommandHandler> logger
) : ICommandHandler<UpdateDishCommand, UpdateDishResponse>
{
    public async Task<Result<UpdateDishResponse>> Handle(
        UpdateDishCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dish = await dishRepository.GetByIdAsync(request.Id, cancellationToken);
            if (dish is null || dish.IsDeleted)
            {
                return Result.Failure<UpdateDishResponse>(Error.NullValue, "Dish not found.");
            }

            var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category is null || category.IsDeleted)
            {
                return Result.Failure<UpdateDishResponse>(Error.NullValue, "Category not found.");
            }

            dish.Update(
                request.Name.Trim(),
                request.Description.Trim(),
                Money.Create(request.Price),
                request.CategoryId,
                request.IsActive,
                currentUserService.UserId);

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            dishRepository.Update(dish);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new UpdateDishResponse
            {
                Id = dish.Id,
                Name = dish.Name,
                Description = dish.Description,
                Price = dish.Price.Amount,
                IsActive = dish.IsActive,
                CategoryId = dish.CategoryId
            };

            return Result.Success(response, "Dish updated successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error updating dish {DishId}", request.Id);
            return Result.Failure<UpdateDishResponse>(
                Error.ServerError,
                "An error occurred while updating dish.");
        }
    }
}
