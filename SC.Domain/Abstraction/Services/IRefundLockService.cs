namespace SC.Domain.Abstraction.Services;

public interface IRefundLockService
{
    Task LockRefundRequestAsync(Guid refundRequestId, CancellationToken cancellationToken = default);

    Task LockOrderRefundRequestsAsync(Guid orderId, CancellationToken cancellationToken = default);
}
