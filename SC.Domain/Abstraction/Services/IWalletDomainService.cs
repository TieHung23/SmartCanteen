using SC.Contract.Shared;

namespace SC.Domain.Abstraction.Services;

public interface IWalletDomainService
{
    Task<Result<decimal>> TryDebitUserBalanceAsync(Guid userId, decimal amount, CancellationToken cancellationToken = default);
    Task<Result<decimal>> TryCreditUserBalanceAsync(Guid userId, decimal amount, CancellationToken cancellationToken = default);
    Task LockUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
