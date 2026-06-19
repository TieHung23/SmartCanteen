using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Services;

namespace SC.Persistence.Database.Services;

public class WalletDomainService(
    SmartCanteenDbContext context,
    ILogger<WalletDomainService> logger) : IWalletDomainService
{
    public async Task LockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
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

    public async Task<Result<decimal>> TryDebitUserBalanceAsync(
        Guid userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
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

        if (result is null or DBNull)
        {
            return Result.Failure<decimal>(Error.InvalidValue, "Insufficient balance.");
        }

        return Result.Success<decimal>(Convert.ToDecimal(result), "Balance debited successfully.");
    }

    public async Task<Result<decimal>> TryCreditUserBalanceAsync(
        Guid userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
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
        return result is null or DBNull
            ? Result.Failure<decimal>(Error.NullValue, "User not found.")
            : Result.Success<decimal>(Convert.ToDecimal(result), "Balance credited successfully.");
    }
}
