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

namespace SC.Application.MediatR.Session.CreateSession;

internal class CreateSessionCommandHandler(
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<CreateSessionCommandHandler> logger
) : ICommandHandler<CreateSessionCommand, CreateSessionResponse>
{
    public async Task<Result<CreateSessionResponse>> Handle(
        CreateSessionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result.Failure<CreateSessionResponse>(
                    Error.InvalidValue,
                    "Session name is required.");
            }

            if (request.AvailableFrom >= request.AvailableTo)
            {
                return Result.Failure<CreateSessionResponse>(
                    Error.InvalidValue,
                    "AvailableFrom must be before AvailableTo.");
            }

            if (request.AvailableForOrder > request.AvailableFrom)
            {
                return Result.Failure<CreateSessionResponse>(
                    Error.InvalidValue,
                    "AvailableForOrder must be before or equal to AvailableFrom.");
            }

            if (!Enum.IsDefined(typeof(AutoFinalizePolicy), request.AutoFinalizePolicy))
            {
                return Result.Failure<CreateSessionResponse>(
                    Error.InvalidValue,
                    "AutoFinalizePolicy is invalid.");
            }

            if (request.FinalizationDeadline.HasValue
                && request.FinalizationDeadline.Value <= DateTimeOffset.UtcNow)
            {
                return Result.Failure<CreateSessionResponse>(
                    Error.InvalidValue,
                    "FinalizationDeadline must be in the future.");
            }

            var hasOverlappingSession = await sessionRepository.ExistsAsync(
                x => !x.IsDeleted
                     && x.AvailableFrom < request.AvailableTo
                     && request.AvailableFrom < x.AvailableTo,
                cancellationToken);

            if (hasOverlappingSession)
            {
                return Result.Failure<CreateSessionResponse>(
                    Error.InvalidValue,
                    "Session time overlaps with another session.");
            }

            var currentUserId = currentUserService.UserId;

            var session = SessionAggregateRoot.Create(
                request.Name,
                request.Description,
                request.AvailableFrom,
                request.AvailableTo,
                request.AvailableForOrder,
                currentUserId);

            if (request.FinalizationDeadline.HasValue)
            {
                session.ConfigureFinalization(
                    request.FinalizationDeadline.Value,
                    (AutoFinalizePolicy)request.AutoFinalizePolicy,
                    currentUserId);
            }

            foreach (var templateInput in request.MealTemplates)
            {
                if (string.IsNullOrWhiteSpace(templateInput.Name))
                {
                    return Result.Failure<CreateSessionResponse>(
                        Error.InvalidValue,
                        "Template name is required.");
                }

                var template = MealTemplate.Create(session.Id, templateInput.Name);

                foreach (var setting in templateInput.Settings)
                {
                    if (setting.MinQuantity < 0)
                    {
                        return Result.Failure<CreateSessionResponse>(
                            Error.InvalidValue,
                            $"MinQuantity for category {setting.CategoryId} cannot be negative.");
                    }

                    if (setting.MaxQuantity < setting.MinQuantity)
                    {
                        return Result.Failure<CreateSessionResponse>(
                            Error.InvalidValue,
                            $"MaxQuantity for category {setting.CategoryId} must be >= MinQuantity.");
                    }

                    template.AddSetting(setting.CategoryId, setting.MinQuantity, setting.MaxQuantity, setting.IsRequired);
                }

                session.AddMealTemplate(template);
            }

            foreach (var dishInput in request.Dishes)
            {
                var dish = await dishRepository.GetByIdAsync(dishInput.DishId, cancellationToken);
                if (dish is null || dish.IsDeleted || !dish.IsActive)
                {
                    return Result.Failure<CreateSessionResponse>(
                        Error.NullValue,
                        $"Dish with id {dishInput.DishId} not found or inactive.");
                }

                session.AddSessionDish(SessionDish.Create(
                    dishInput.DishId,
                    session.Id));
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await sessionRepository.AddAsync(session, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new CreateSessionResponse
            {
                Id = session.Id,
                Name = session.Name,
                Description = session.Description,
                IsActive = session.IsActive,
                AvailableFrom = session.AvailableFrom,
                AvailableTo = session.AvailableTo,
                AvailableForOrder = session.AvailableForOrder,
                FinalizationDeadline = session.FinalizationDeadline,
                AutoFinalizePolicy = (int)session.AutoFinalizePolicy,
                CreatedAtUtc = session.CreatedAtUtc,
                CreatedBy = session.CreatedBy,
                Message = "Session created successfully."
            };

            return Result.Success(response, "Session created successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error creating session");
            return Result.Failure<CreateSessionResponse>(
                Error.ServerError,
                "An error occurred while creating the session.");
        }
    }
}
