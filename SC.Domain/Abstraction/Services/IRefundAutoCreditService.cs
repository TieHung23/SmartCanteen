using SC.Contract.Shared;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.SharedKernel.ValueObjects;

namespace SC.Domain.Abstraction.Services;

public interface IRefundAutoCreditService
{
    Task<Result<RefundAutoCreditResult>> CreditAsync(
        RefundRequest refund,
        CancellationToken cancellationToken = default);
}
