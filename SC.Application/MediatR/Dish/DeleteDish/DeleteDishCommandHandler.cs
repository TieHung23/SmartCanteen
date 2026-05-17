using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Dish.DeleteDish;

internal class DeleteDishCommandHandler(
    IRepositoryBase<DishAggregateRoot, Guid> dishRepository,
    ICurrentUserService currentUserService,
    ILogger<DeleteDishCommandHandler> logger
) : ICommandHandler<DeleteDishCommand>
{
    public async Task<Result> Handle(
        DeleteDishCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dish = await dishRepository.FindByIdAsync(request.Id, cancellationToken);

            if (dish is null || dish.IsDeleted)
            {
                return Result.Failure(Error.NullValue, "Dish not found.");
            }

            dish.SoftDelete(currentUserService.UserId);

            var deleteResult = await dishRepository.UpdateAsync(dish);
            if (deleteResult.IsFailure)
            {
                return Result.Failure(
                    deleteResult.Error ?? Error.ServerError,
                    deleteResult.Message);
            }

            return Result.Success("Dish deleted successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting dish {DishId}", request.Id);
            return Result.Failure(
                Error.ServerError,
                "An error occurred while deleting dish.");
        }
    }
}
