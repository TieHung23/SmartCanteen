using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Cart;

public interface ICartValidationService
{
    Task<Result<ValidatedCart>> ValidateAsync(
        CartData? data,
        bool requireCompleteTemplate = true,
        CancellationToken cancellationToken = default);
}

public sealed class ValidatedCart
{
    public required IReadOnlyDictionary<Guid, SessionAggregateRoot> Sessions { get; init; }
    public required IReadOnlyDictionary<Guid, DishAggregateRoot> Dishes { get; init; }
}

public sealed class CartValidationService(
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository)
    : ICartValidationService
{
    public async Task<Result<ValidatedCart>> ValidateAsync(
        CartData? data,
        bool requireCompleteTemplate = true,
        CancellationToken cancellationToken = default)
    {
        if (data is null)
        {
            return Result.Failure<ValidatedCart>(
                Error.NullValue,
                "Cart data is required.");
        }

        if (data.Sessions.Count == 0)
        {
            return Result.Failure<ValidatedCart>(
                Error.InvalidValue,
                "Cart must contain at least one session.");
        }

        if (data.Sessions.GroupBy(x => x.SessionId).Any(group => group.Count() > 1))
        {
            return Result.Failure<ValidatedCart>(
                Error.InvalidValue,
                "Cart cannot contain duplicate sessions.");
        }

        foreach (var sessionData in data.Sessions)
        {
            var basicResult = ValidateBasicSessionData(sessionData);
            if (basicResult.IsFailure)
            {
                return Result.Failure<ValidatedCart>(
                    basicResult.Error!,
                    basicResult.Message);
            }

            if (sessionData.Items!.GroupBy(x => x.DishId).Any(group => group.Count() > 1))
            {
                return Result.Failure<ValidatedCart>(
                    Error.InvalidValue,
                    "Cart cannot contain duplicate dishes in the same session.");
            }
        }

        var sessionIds = data.Sessions.Select(x => x.SessionId).ToList();
        var sessionsList = await sessionRepository
            .FindListAsync(x => sessionIds.Contains(x.Id), cancellationToken,
                x => x.SessionDishes, x => x.MealTemplates);
        var sessions = sessionsList.ToDictionary(x => x.Id);

        if (sessions.Count != sessionIds.Count)
        {
            return Result.Failure<ValidatedCart>(
                Error.NullValue,
                "One or more sessions were not found.");
        }

        var dishIds = data.Sessions
            .SelectMany(x => x.Items!)
            .Select(x => x.DishId)
            .Distinct()
            .ToList();
        var dishesList = await dishRepository
            .FindListAsync(x => dishIds.Contains(x.Id), cancellationToken);
        var dishes = dishesList.ToDictionary(x => x.Id);

        if (dishes.Count != dishIds.Count)
        {
            return Result.Failure<ValidatedCart>(
                Error.NullValue,
                "One or more dishes were not found.");
        }

        if (dishes.Values.Any(x => x.IsDeleted || !x.IsActive))
        {
            return Result.Failure<ValidatedCart>(
                Error.InvalidValue,
                "One or more dishes are not available.");
        }

        foreach (var sessionData in data.Sessions)
        {
            var session = sessions[sessionData.SessionId];
            var items = sessionData.Items!;

            if (session.IsDeleted || !session.IsActive)
            {
                return Result.Failure<ValidatedCart>(
                    Error.InvalidValue,
                    "One or more sessions are not available.");
            }

            if (DateTimeOffset.UtcNow > session.AvailableForOrder)
            {
                return Result.Failure<ValidatedCart>(
                    Error.InvalidValue,
                    "One or more sessions have passed the ordering deadline.");
            }

            var sessionDishes = session.SessionDishes.ToDictionary(x => x.DishId);
            var sessionDishIds = items.Select(x => x.DishId).ToList();
            if (sessionDishIds.Any(dishId => !sessionDishes.ContainsKey(dishId)))
            {
                return Result.Failure<ValidatedCart>(
                    Error.InvalidValue,
                    "One or more dishes do not belong to the selected session.");
            }

            var template = session.MealTemplates.SingleOrDefault(x =>
                x.Id == sessionData.MealTemplateId && !x.IsDeleted);
            if (template is null)
            {
                return Result.Failure<ValidatedCart>(
                    Error.InvalidValue,
                    "Meal template does not belong to the selected session.");
            }

            var templateRuleResult = CartTemplateRuleValidator.Validate(
                template,
                items,
                dishes,
                requireCompleteTemplate);
            if (templateRuleResult.IsFailure)
            {
                return Result.Failure<ValidatedCart>(
                    templateRuleResult.Error!,
                    templateRuleResult.Message);
            }
        }

        return Result.Success(
            new ValidatedCart
            {
                Sessions = sessions,
                Dishes = dishes
            },
            "Cart data is valid.");
    }

    private static Result ValidateBasicSessionData(CartSessionData sessionData)
    {
        if (sessionData.SessionId == Guid.Empty)
        {
            return Result.Failure(
                Error.InvalidValue,
                "SessionId is required.");
        }

        if (sessionData.MealTemplateId == Guid.Empty)
        {
            return Result.Failure(
                Error.InvalidValue,
                "MealTemplateId is required.");
        }

        if (sessionData.Items is null || sessionData.Items.Count == 0)
        {
            return Result.Failure(
                Error.InvalidValue,
                "Cart session must contain at least one item.");
        }

        if (sessionData.Items.Any(x => x.DishId == Guid.Empty))
        {
            return Result.Failure(
                Error.InvalidValue,
                "DishId is required for every cart item.");
        }

        if (sessionData.Items.Any(x => x.Quantity <= 0))
        {
            return Result.Failure(
                Error.InvalidValue,
                "Item quantity must be greater than zero.");
        }

        return Result.Success("Cart session data is valid.");
    }
}
