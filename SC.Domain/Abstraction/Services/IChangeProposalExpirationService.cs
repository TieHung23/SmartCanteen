using SC.Contract.Shared;

namespace SC.Domain.Abstraction.Services;

public interface IChangeProposalExpirationService : IDomainService
{
    Task<Result> ProcessExpiredProposalsAsync(CancellationToken cancellationToken = default);
}
