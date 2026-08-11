using Microsoft.Extensions.Logging;
using SC.Application.MediatR.Session;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Dish.DeleteDish;

internal class DeleteDishCommandHandler(
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<DeleteDishCommandHandler> logger
) : ICommandHandler<DeleteDishCommand, DeleteDishResponse>
{
    public async Task<Result<DeleteDishResponse>> Handle(
        DeleteDishCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dish = await dishRepository.GetByIdAsync(request.Id, cancellationToken);

            if (dish is null || dish.IsDeleted)
            {
                return Result.Failure<DeleteDishResponse>(
                    Error.NullValue,
                    "Dish not found.");
            }

            var blockingSessions = await CurrentSessionUsage.FindSessionNamesUsingDishAsync(
                sessionRepository, request.Id, cancellationToken);
            if (blockingSessions.Count > 0)
            {
                return Result.Failure<DeleteDishResponse>(
                    Error.ResourceBusy,
                    "Cannot delete a dish that belongs to a current session.",
                    CurrentSessionUsage.Describe(blockingSessions));
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            dish.SoftDelete();
            dishRepository.Update(dish);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new DeleteDishResponse
            {
                Id = request.Id,
                Message = "Dish deleted successfully."
            };

            return Result.Success(response, response.Message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error deleting dish {DishId}", request.Id);
            return Result.Failure<DeleteDishResponse>(
                Error.ServerError,
                "An error occurred while deleting dish.");
        }
    }
}
