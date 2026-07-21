using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Dish;
using SC.Domain.Domain.Dish.AggregateRoot;
using SC.Domain.Domain.Session.Entity;
using SC.Domain.Domain.Session.Enum;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Session.UpdateSession;

internal class UpdateSessionCommandHandler(
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<MealTemplate, Guid> mealTemplateRepository,
    IGenericRepository<SessionDish, Guid> sessionDishRepository,
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
            var session = await sessionRepository.FindSingleAsync(
                x => x.Id == request.Id,
                q => q.Include(x => x.MealTemplates)
                    .ThenInclude(x => x.Settings),
                cancellationToken);

            if (session is null)
            {
                return Result.Failure<UpdateSessionResponse>(
                    Error.NullValue,
                    $"Session with id {request.Id} not found.");
            }

            var now = DateTimeOffset.UtcNow;

            if (now >= session.AvailableForOrder)
            {
                return Result.Failure<UpdateSessionResponse>(
                    Error.InvalidValue,
                    "Session cannot be updated after ordering has opened.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result.Failure<UpdateSessionResponse>(
                    Error.InvalidValue,
                    "Session name is required.");
            }

            if (request.AvailableFrom >= request.AvailableTo)
            {
                return Result.Failure<UpdateSessionResponse>(
                    Error.InvalidValue,
                    "AvailableFrom must be before AvailableTo.");
            }

            if (request.AvailableForOrder > request.AvailableFrom)
            {
                return Result.Failure<UpdateSessionResponse>(
                    Error.InvalidValue,
                    "AvailableForOrder must be before or equal to AvailableFrom.");
            }

            if (!Enum.IsDefined(typeof(AutoFinalizePolicy), request.AutoFinalizePolicy))
            {
                return Result.Failure<UpdateSessionResponse>(
                    Error.InvalidValue,
                    "AutoFinalizePolicy is invalid.");
            }

            if (request.FinalizationDeadline.HasValue
                && request.FinalizationDeadline.Value <= now)
            {
                return Result.Failure<UpdateSessionResponse>(
                    Error.InvalidValue,
                    "FinalizationDeadline must be in the future.");
            }

            if (session.IsFinalized)
            {
                return Result.Failure<UpdateSessionResponse>(
                    Error.InvalidValue,
                    "Finalized session cannot be updated.");
            }

            var hasOverlappingSession = await sessionRepository.ExistsAsync(
                x => !x.IsDeleted
                     && x.Id != request.Id
                     && x.AvailableFrom < request.AvailableTo
                     && request.AvailableFrom < x.AvailableTo,
                cancellationToken);

            if (hasOverlappingSession)
            {
                return Result.Failure<UpdateSessionResponse>(
                    Error.InvalidValue,
                    "Session time overlaps with another session.");
            }

            var currentUserId = currentUserService.UserId;

            var replacementMealTemplates = new List<MealTemplate>();
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

                replacementMealTemplates.Add(template);
            }

            var requestedDishIds = new HashSet<Guid>();
            foreach (var dishInput in request.Dishes)
            {
                if (!requestedDishIds.Add(dishInput.DishId))
                {
                    return Result.Failure<UpdateSessionResponse>(
                        Error.InvalidValue,
                        $"Dish {dishInput.DishId} is duplicated in the session.");
                }

                var dish = await dishRepository.GetByIdAsync(dishInput.DishId, cancellationToken);
                if (dish is null || dish.IsDeleted || !dish.IsActive)
                {
                    return Result.Failure<UpdateSessionResponse>(
                        Error.NullValue,
                        $"Dish with id {dishInput.DishId} not found or inactive.");
                }
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);

            var existingMealTemplates = session.MealTemplates.ToList();
            var existingSessionDishes = await sessionDishRepository.FindListAsync(
                x => x.SessionId == session.Id,
                cancellationToken);
            var sessionDishesToRemove = existingSessionDishes
                .Where(x => !requestedDishIds.Contains(x.DishId))
                .ToList();
            var existingDishIds = existingSessionDishes
                .Select(x => x.DishId)
                .ToHashSet();
            var sessionDishesToAdd = requestedDishIds
                .Where(dishId => !existingDishIds.Contains(dishId))
                .Select(dishId => SessionDish.Create(dishId, session.Id))
                .ToList();

            session.Update(
                request.Name,
                request.Description,
                request.AvailableFrom,
                request.AvailableTo,
                request.AvailableForOrder,
                request.IsActive,
                currentUserId);

            session.ConfigureFinalization(
                request.FinalizationDeadline,
                (AutoFinalizePolicy)request.AutoFinalizePolicy,
                currentUserId);

            mealTemplateRepository.DeleteRange(existingMealTemplates);
            sessionDishRepository.DeleteRange(sessionDishesToRemove);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var template in replacementMealTemplates)
            {
                await mealTemplateRepository.AddAsync(template, cancellationToken);
            }

            foreach (var sessionDish in sessionDishesToAdd)
            {
                await sessionDishRepository.AddAsync(sessionDish, cancellationToken);
            }

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
