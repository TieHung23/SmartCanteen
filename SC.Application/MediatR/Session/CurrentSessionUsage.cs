using SC.Domain.Abstraction.Repositories;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Session;

/// <summary>
/// Guards menu edits against sessions that are still live. A session counts as
/// "current" while it is active and its serving window has not closed yet, which
/// covers both sessions still open for ordering and sessions already being served.
/// Past sessions are history and never block an edit.
/// </summary>
internal static class CurrentSessionUsage
{
    public static async Task<List<string>> FindSessionNamesUsingDishAsync(
        IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
        Guid dishId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var sessions = await sessionRepository.FindListAsync(
            session => !session.IsDeleted
                       && session.IsActive
                       && session.AvailableTo >= now
                       && session.SessionDishes.Any(sessionDish => sessionDish.DishId == dishId),
            cancellationToken);

        return sessions.Select(session => session.Name).ToList();
    }

    /// <summary>
    /// A category is in use by a current session either through one of its dishes on
    /// the session menu, or through a meal template setting that requires the category.
    /// </summary>
    public static async Task<List<string>> FindSessionNamesUsingCategoryAsync(
        IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
        IGenericRepository<DishAggregateRoot, Guid> dishRepository,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var dishes = await dishRepository.FindListAsync(
            dish => dish.CategoryId == categoryId && !dish.IsDeleted,
            cancellationToken);
        var dishIds = dishes.Select(dish => dish.Id).ToList();

        var sessions = await sessionRepository.FindListAsync(
            session => !session.IsDeleted
                       && session.IsActive
                       && session.AvailableTo >= now
                       && (session.SessionDishes.Any(sessionDish => dishIds.Contains(sessionDish.DishId))
                           || session.MealTemplates.Any(template =>
                               !template.IsDeleted
                               && template.Settings.Any(setting =>
                                   !setting.IsDeleted && setting.CategoryId == categoryId))),
            cancellationToken);

        return sessions.Select(session => session.Name).ToList();
    }

    public static string Describe(IReadOnlyCollection<string> sessionNames)
    {
        return $"Currently used by: {string.Join(", ", sessionNames)}.";
    }
}
