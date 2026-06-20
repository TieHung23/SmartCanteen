using SC.Domain.Abstraction.Repositories;

namespace SC.Persistence.Database.Repository;

public class UnitOfWork(SmartCanteenDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await context.Database.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction is not null)
        {
            await context.Database.RollbackTransactionAsync(cancellationToken);
        }
    }
}
