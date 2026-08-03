using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.WalletTransaction.Enum;
using UserAggregate = SC.Domain.Domain.User.User;
using WalletTransactionEntity = SC.Domain.Domain.WalletTransaction.Entity.WalletTransaction;

namespace SC.Persistence.Database.Services;

public sealed class RefundAutoCreditService(
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<WalletTransactionEntity, Guid> walletTransactionRepository,
    IWalletDomainService walletDomainService) : IRefundAutoCreditService
{
    public async Task<Result<RefundAutoCreditResult>> CreditAsync(
        RefundRequest refund,
        CancellationToken cancellationToken = default)
    {
        await walletDomainService.LockUserAsync(refund.UserId, cancellationToken);
        var user = await userRepository.GetByIdAsync(refund.UserId, cancellationToken);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure<RefundAutoCreditResult>(
                Error.NullValue,
                "Refund request user not found.");
        }

        var balanceBefore = user.Balance.Amount;
        var creditResult = await walletDomainService.TryCreditUserBalanceAsync(
            user.Id,
            refund.RefundAmount,
            cancellationToken);

        if (creditResult.IsFailure)
        {
            return Result.Failure<RefundAutoCreditResult>(
                creditResult.Error ?? Error.ServerError,
                creditResult.Message);
        }

        var balanceAfter = creditResult.Value;
        var walletTransaction = WalletTransactionEntity.Create(
            user.Id,
            refund.RefundAmount,
            balanceBefore,
            balanceAfter,
            WalletTransactionType.Refund);

        refund.AutoApprove(walletTransaction.Id);

        await walletTransactionRepository.AddAsync(walletTransaction, cancellationToken);

        return Result.Success(
            new RefundAutoCreditResult(walletTransaction.Id, balanceAfter),
            "Refund credited automatically.");
    }
}
