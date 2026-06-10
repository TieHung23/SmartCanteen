using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Domain.Abstraction.Repositories;

namespace SC.Persistence.Database.Repository;

public class UnitOfWork(SmartCanteenDbContext context, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private readonly SmartCanteenDbContext _context = context;
    private readonly ILogger<UnitOfWork> _logger = logger;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving changes to database");
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Beginning database transaction");
        await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task LockRefundRequestAsync(
        Guid refundRequestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Locking refund request {RefundRequestId}",
            refundRequestId);
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM "RefundRequests"
             WHERE "Id" = {refundRequestId}
             FOR UPDATE
             """,
            cancellationToken);
    }

    public async Task LockUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Locking user wallet {UserId}", userId);
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM "Users"
             WHERE "Id" = {userId}
             FOR UPDATE
             """,
            cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Committing database transaction");
        await _context.Database.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rolling back database transaction");
        if (_context.Database.CurrentTransaction is not null)
        {
            await _context.Database.RollbackTransactionAsync(cancellationToken);
        }
    }
}
