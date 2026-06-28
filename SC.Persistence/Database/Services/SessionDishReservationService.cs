using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Services;

namespace SC.Persistence.Database.Services;

public class SessionDishReservationService(SmartCanteenDbContext context) : ISessionDishReservationService
{
    public async Task<Result> TryReserveAsync(
        Guid sessionId,
        Guid dishId,
        int quantity,
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
            UPDATE "SessionDish"
            SET "Quantity" = "Quantity" - @quantity
            WHERE "SessionId" = @sessionId
              AND "DishId" = @dishId
              AND "Quantity" >= @quantity
            RETURNING "Quantity";
            """;

        var sessionIdParameter = command.CreateParameter();
        sessionIdParameter.ParameterName = "sessionId";
        sessionIdParameter.Value = sessionId;
        command.Parameters.Add(sessionIdParameter);

        var dishIdParameter = command.CreateParameter();
        dishIdParameter.ParameterName = "dishId";
        dishIdParameter.Value = dishId;
        command.Parameters.Add(dishIdParameter);

        var quantityParameter = command.CreateParameter();
        quantityParameter.ParameterName = "quantity";
        quantityParameter.Value = quantity;
        command.Parameters.Add(quantityParameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not null and not DBNull
            ? Result.Success("Reserved successfully.")
            : Result.Failure(Error.InsufficientDishStock, "One or more dishes ran out of stock.");
    }
}
