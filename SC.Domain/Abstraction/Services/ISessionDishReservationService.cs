using SC.Contract.Shared;

namespace SC.Domain.Abstraction.Services;

public interface ISessionDishReservationService
{
    Task<Result> TryReserveAsync(Guid sessionId, Guid dishId, int quantity, CancellationToken cancellationToken = default);
}
