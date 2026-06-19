using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Dish;
using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.Session.AggregateRoot;
using SC.Domain.Domain.Session.Enum;

namespace SC.Persistence.Database.Services;

public class FinalizeSessionService(
    SmartCanteenDbContext context,
    IGenericRepository<Session, Guid> sessionRepository,
    IGenericRepository<Order, Guid> orderRepository,
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    ILogger<FinalizeSessionService> logger) : IFinalizeSessionService
{
    public async Task<Result> FinalizeAsync(Guid sessionId, List<(Guid DishId, int PreparedQuantity)> preparedDishes, Guid managerId, CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.FindSingleAsync(
            s => s.Id == sessionId && !s.IsDeleted,
            cancellationToken);

        if (session is null)
            return Result.Failure(Error.NullValue, "Session not found.");

        try
        {
            if (session.FinalizationDeadline.HasValue && DateTimeOffset.UtcNow > session.FinalizationDeadline.Value)
                return Result.Failure(Error.InvalidValue, "Finalization deadline has passed.");
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.InvalidValue, ex.Message);
        }

        foreach (var input in preparedDishes)
        {
            var sd = session.SessionDishes.FirstOrDefault(x => x.DishId == input.DishId);
            if (sd is null)
                return Result.Failure(Error.NullValue, $"Dish {input.DishId} is not part of this session.");

            sd.SetPreparedQuantity(input.PreparedQuantity);
        }

        var orders = await orderRepository.FindListAsync(
            o => o.SessionId == sessionId,
            cancellationToken);

        foreach (var order in orders)
        {
            foreach (var item in order.OrderItems)
            {
                var sd = session.SessionDishes.FirstOrDefault(x => x.DishId == item.DishId);

                if (sd is not null && sd.HasEnoughFor(item.Quantity))
                {
                    item.Confirm();
                }
                else
                {
                    item.MarkChangePending();

                    var proposal = OrderItemChangeProposal.Create(
                        order.Id,
                        order.CreatedBy,
                        item.DishId,
                        null);

                    await proposalRepository.AddAsync(proposal, cancellationToken);
                }
            }

            orderRepository.Update(order);
        }

        session.Finalize(managerId);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success("Session finalized successfully.");
    }

    public async Task AutoFinalizeOverdueSessionsAsync(CancellationToken cancellationToken = default)
    {
        var overdueSessions = await sessionRepository.FindListAsync(
            s => !s.IsDeleted
                 && !s.IsFinalized
                 && s.FinalizationDeadline.HasValue
                 && s.FinalizationDeadline <= DateTimeOffset.UtcNow,
            cancellationToken);

        foreach (var session in overdueSessions)
        {
            try
            {
                session.AutoFinalize();
                sessionRepository.Update(session);

                var orders = await orderRepository.FindListAsync(
                    o => o.SessionId == session.Id,
                    cancellationToken);

                foreach (var order in orders)
                {
                    foreach (var item in order.OrderItems)
                    {
                        if (session.AutoFinalizePolicy == AutoFinalizePolicy.AutoReject)
                        {
                            item.RefundItem();
                        }
                    }

                    orderRepository.Update(order);
                }

                logger.LogInformation(
                    "Auto-finalized session {SessionId} with policy {Policy}",
                    session.Id, session.AutoFinalizePolicy);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to auto-finalize session {SessionId}",
                    session.Id);
            }
        }

        if (overdueSessions.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
