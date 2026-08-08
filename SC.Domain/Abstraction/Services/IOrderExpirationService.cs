using SC.Contract.Shared;

namespace SC.Domain.Abstraction.Services;

public interface IOrderExpirationService : IDomainService
{
    /// <summary>
    /// Sweeps sessions whose serving window (AvailableTo) has already closed: deactivates each such
    /// session (IsActive = false), and marks any of its orders that were never cancelled or collected
    /// as Expired instead of leaving them stuck in Pending/Preparing/ReadyForPickup forever with no
    /// one left to serve or pick them up.
    /// </summary>
    Task<Result> ExpireOverdueOrdersAsync(CancellationToken cancellationToken = default);
}
