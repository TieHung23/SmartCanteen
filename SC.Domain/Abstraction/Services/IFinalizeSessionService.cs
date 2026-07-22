using SC.Contract.Shared;

namespace SC.Domain.Abstraction.Services;

public interface IFinalizeSessionService : IDomainService
{
    Task<Result> FinalizeAsync(Guid sessionId, List<(Guid DishId, int PreparedQuantity, Guid? SuggestedDishId)> preparedDishes, Guid managerId, CancellationToken cancellationToken = default);
    Task AutoFinalizeOverdueSessionsAsync(CancellationToken cancellationToken = default);
}
