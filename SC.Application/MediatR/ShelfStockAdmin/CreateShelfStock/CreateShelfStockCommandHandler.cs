using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using ShelfStockEntity = SC.Domain.Domain.ShelfStock.Entity.ShelfStock;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.ShelfStockAdmin.CreateShelfStock;

internal sealed class CreateShelfStockCommandHandler(
    IGenericRepository<ShelfStockEntity, Guid> shelfStockRepository,
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<CreateShelfStockCommandHandler> logger
) : ICommandHandler<CreateShelfStockCommand, CreateShelfStockResponse>
{
    public async Task<Result<CreateShelfStockResponse>> Handle(
        CreateShelfStockCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.Quantity < 0)
            {
                return Result.Failure<CreateShelfStockResponse>(
                    Error.InvalidValue, "Quantity must be >= 0.");
            }

            var session = await sessionRepository.FindSingleAsync(
                x => x.Id == request.SessionId && !x.IsDeleted, cancellationToken);
            if (session is null)
            {
                return Result.Failure<CreateShelfStockResponse>(
                    Error.SessionNotFound, "Session was not found.");
            }

            var dish = await dishRepository.FindSingleAsync(
                x => x.Id == request.DishId && !x.IsDeleted, cancellationToken);
            if (dish is null)
            {
                return Result.Failure<CreateShelfStockResponse>(
                    Error.DishNotFound, "Dish was not found.");
            }

            // 1 dòng tồn / cặp (session, dish)
            var existing = await shelfStockRepository.FindSingleAsync(
                x => x.SessionId == request.SessionId
                     && x.DishId == request.DishId
                     && !x.IsDeleted,
                cancellationToken);
            if (existing is not null)
            {
                return Result.Failure<CreateShelfStockResponse>(
                    Error.CodeAlreadyExists,
                    "Shelf stock for this dish already exists in this session. Use refill instead.");
            }

            var stock = ShelfStockEntity.Create(
                request.SessionId, request.DishId, request.Quantity,
                currentUserService.UserId, request.SlotConfigurationId);

            await shelfStockRepository.AddAsync(stock, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new CreateShelfStockResponse(
                    stock.Id, stock.SessionId, stock.DishId, stock.Quantity, stock.SlotConfigurationId),
                "Shelf stock created.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating shelf stock");
            return Result.Failure<CreateShelfStockResponse>(
                Error.ServerError, "An error occurred while creating shelf stock.");
        }
    }
}
