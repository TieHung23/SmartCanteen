using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

    public async Task LockUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText =
            """
            SELECT 1
            FROM "Users"
            WHERE "Id" = @userId
            FOR UPDATE
            """;

        var userIdParameter = command.CreateParameter();
        userIdParameter.ParameterName = "userId";
        userIdParameter.Value = userId;
        command.Parameters.Add(userIdParameter);

        await command.ExecuteScalarAsync(cancellationToken);
    }

    public async Task<decimal?> TryDebitUserBalanceAsync(
        Guid userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText =
            """
            UPDATE "Users"
            SET "Balance_Amount" = "Balance_Amount" - @amount
            WHERE "Id" = @userId
              AND "Balance_Amount" >= @amount
            RETURNING "Balance_Amount";
            """;

        var userIdParameter = command.CreateParameter();
        userIdParameter.ParameterName = "userId";
        userIdParameter.Value = userId;
        command.Parameters.Add(userIdParameter);

        var amountParameter = command.CreateParameter();
        amountParameter.ParameterName = "amount";
        amountParameter.Value = amount;
        command.Parameters.Add(amountParameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : Convert.ToDecimal(result);
    }

    public async Task<decimal?> TryCreditUserBalanceAsync(
        Guid userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText =
            """
            UPDATE "Users"
            SET "Balance_Amount" = "Balance_Amount" + @amount
            WHERE "Id" = @userId
            RETURNING "Balance_Amount";
            """;

        var userIdParameter = command.CreateParameter();
        userIdParameter.ParameterName = "userId";
        userIdParameter.Value = userId;
        command.Parameters.Add(userIdParameter);

        var amountParameter = command.CreateParameter();
        amountParameter.ParameterName = "amount";
        amountParameter.Value = amount;
        command.Parameters.Add(amountParameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : Convert.ToDecimal(result);
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
