namespace SC.Domain.Abstraction.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task LockUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<decimal?> TryDebitUserBalanceAsync(
        Guid userId,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task<decimal?> TryCreditUserBalanceAsync(
        Guid userId,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task<bool> TryReserveMealDishAsync(
        Guid mealId,
        Guid dishId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task LockRefundRequestAsync(Guid refundRequestId, CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
