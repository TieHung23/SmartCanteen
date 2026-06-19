using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Dish;
using SC.Domain.Domain.Dish.AggregateRoot;
using SC.Domain.Domain.Session.Entity;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Session.UpdateSession;

internal class UpdateSessionCommandHandler(
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<UpdateSessionCommandHandler> logger
) : ICommandHandler<UpdateSessionCommand, UpdateSessionResponse>
{
    public async Task<Result<UpdateSessionResponse>> Handle(
        UpdateSessionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await sessionRepository.GetByIdAsync(request.Id, cancellationToken);

            if (session is null)
            {
                return Result.Failure<UpdateSessionResponse>(
                    Error.NullValue,
                    $"Session with id {request.Id} not found.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result.Failure<UpdateSessionResponse>(
                    Error.InvalidValue,
                    "Session name is required.");
            }

            var currentUserId = currentUserService.UserId;

            session.Update(
                request.Name,
                request.Description,
                request.AvailableFrom,
                request.AvailableTo,
                request.AvailableForOrder,
                request.IsActive,
                currentUserId);

            // Update meal templates
            session.ClearMealTemplates();
            foreach (var templateInput in request.MealTemplates)
            {
                if (string.IsNullOrWhiteSpace(templateInput.Name))
                {
                    return Result.Failure<UpdateSessionResponse>(
                        Error.InvalidValue,
                        "Template name is required.");
                }

                var template = MealTemplate.Create(session.Id, templateInput.Name);

                foreach (var setting in templateInput.Settings)
                {
                    if (setting.MinQuantity < 0)
                    {
                        return Result.Failure<UpdateSessionResponse>(
                            Error.InvalidValue,
                            $"MinQuantity for category {setting.CategoryId} cannot be negative.");
                    }

                    if (setting.MaxQuantity < setting.MinQuantity)
                    {
                        return Result.Failure<UpdateSessionResponse>(
                            Error.InvalidValue,
                            $"MaxQuantity for category {setting.CategoryId} must be >= MinQuantity.");
                    }

                    template.AddSetting(setting.CategoryId, setting.MinQuantity, setting.MaxQuantity, setting.IsRequired);
                }

                session.AddMealTemplate(template);
            }

            // Update dish sessions
            session.ClearSessionDishes();
            foreach (var dishInput in request.Dishes)
            {
                var dish = await dishRepository.GetByIdAsync(dishInput.DishId, cancellationToken);
                if (dish is null || dish.IsDeleted || !dish.IsActive)
                {
                    return Result.Failure<UpdateSessionResponse>(
                        Error.NullValue,
                        $"Dish with id {dishInput.DishId} not found or inactive.");
                }

                session.AddSessionDish(SessionDish.Create(
                    dishInput.DishId,
                    session.Id,
                    dishInput.Quantity > 0 ? dishInput.Quantity : 1));
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            sessionRepository.Update(session);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new UpdateSessionResponse
            {
                Id = session.Id,
                Name = session.Name,
                Message = "Session updated successfully."
            };

            return Result.Success(response, "Session updated successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error updating session with id {SessionId}", request.Id);
            return Result.Failure<UpdateSessionResponse>(
                Error.ServerError,
                "An error occurred while updating the session.");
        }
    }
}
