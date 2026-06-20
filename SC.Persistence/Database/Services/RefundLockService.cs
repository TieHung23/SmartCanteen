using Microsoft.EntityFrameworkCore;
using SC.Domain.Abstraction.Services;

namespace SC.Persistence.Database.Services;

public class RefundLockService(SmartCanteenDbContext context) : IRefundLockService
{
    public async Task LockRefundRequestAsync(Guid refundRequestId, CancellationToken cancellationToken = default)
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM "RefundRequests"
             WHERE "Id" = {refundRequestId}
             FOR UPDATE
             """,
            cancellationToken);
    }
}
