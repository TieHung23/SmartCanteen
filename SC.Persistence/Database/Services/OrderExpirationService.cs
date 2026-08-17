using Microsoft.Extensions.Logging;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.ServingJob.Enum;
using SC.Contract.Shared;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;
using PickupSlotEntity = SC.Domain.Domain.PickupSlot.Entity.PickupSlot;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using OrderStatusHistoryEntity = SC.Domain.Domain.OrderStatusHistory.Entity.OrderStatusHistory;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;

namespace SC.Persistence.Database.Services;

public sealed class OrderExpirationService(
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<PickupSlotEntity, Guid> pickupSlotRepository,
    IGenericRepository<OrderStatusHistoryEntity, Guid> orderStatusHistoryRepository,
    IGenericRepository<TrayEntity, Guid> trayRepository,
    IUnitOfWork unitOfWork,
    ILogger<OrderExpirationService> logger) : IOrderExpirationService
{
    public async Task<Result> ExpireOverdueOrdersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var closedSessions = await sessionRepository.FindListAsync(
            s => !s.IsDeleted && s.AvailableTo <= now,
            cancellationToken);

        if (closedSessions.Count == 0)
            return Result.Success("No closed sessions to sweep.");

        var hasChanges = false;

        // A session whose serving window has closed is no longer "operating" - deactivate it so
        // listings/checks reflect that, not just the time-window math callers would otherwise have
        // to redo themselves.
        foreach (var session in closedSessions)
        {
            if (!session.IsActive) continue;

            try
            {
                session.MarkInactive(session.CreatedBy);
                sessionRepository.Update(session);
                hasChanges = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deactivate closed session {SessionId}", session.Id);
            }
        }

        var closedSessionIds = closedSessions.Select(s => s.Id).ToHashSet();

        var staleOrders = await orderRepository.FindListAsync(
            o => closedSessionIds.Contains(o.SessionId)
                 && o.Status != OrderStatus.Cancelled
                 && o.Status != OrderStatus.Completed
                 && o.Status != OrderStatus.Expired,
            cancellationToken);

        foreach (var order in staleOrders)
        {
            try
            {
                await ExpireOrderAsync(order, cancellationToken);
                hasChanges = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to expire overdue order {OrderId}", order.Id);
            }
        }

        if (hasChanges)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success("Overdue orders expired and closed sessions deactivated successfully.");
    }

    private async Task ExpireOrderAsync(OrderAggregateRoot order, CancellationToken cancellationToken)
    {
        var fromStatus = order.Status;
        order.UpdateStatus(OrderStatus.Expired, order.CreatedBy);
        orderRepository.Update(order);
        await orderStatusHistoryRepository.AddAsync(
            OrderStatusHistoryEntity.Create(
                order.Id,
                fromStatus,
                OrderStatus.Expired,
                order.CreatedBy,
                "SessionWindowClosed",
                "Session's serving window ended before the order was collected."),
            cancellationToken);

        // Close out anything still physically in-flight for this order so robots/staff stop treating
        // it as active (mirrors the cleanup ForceClearPickupSlot does for a single manually-cleared slot).
        var activeJobs = await servingJobRepository.FindListAsync(
            j => j.OrderId == order.Id
                 && j.Status != ServingJobStatus.Cancelled
                 && j.Status != ServingJobStatus.Collected,
            cancellationToken);
        foreach (var job in activeJobs)
        {
            // Khay job đang giữ phải VỀ POOL trước khi job đóng — giống dead-order sweep của
            // PullNextJob (bước 1c). Thiếu đoạn này khay kẹt Reserved vĩnh viễn vì job đã
            // Cancelled thì không sweeper nào rờ tới nữa (đo thật: TRAY001 kẹt sáng 17/08).
            if (job.TrayId is Guid heldTrayId)
            {
                var heldTray = await trayRepository.GetByIdAsync(heldTrayId, cancellationToken);
                if (heldTray is not null)
                {
                    heldTray.Release(order.CreatedBy);
                    trayRepository.Update(heldTray);
                }
                job.ClearTray(order.CreatedBy);
            }
            job.Cancel(order.CreatedBy);
            servingJobRepository.Update(job);
        }

        var slot = await pickupSlotRepository.FindSingleAsync(
            s => !s.IsDeleted && s.OrderId == order.Id,
            cancellationToken);
        if (slot is not null)
        {
            slot.Clear(order.CreatedBy);
            pickupSlotRepository.Update(slot);
        }
    }
}
