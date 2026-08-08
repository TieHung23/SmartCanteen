using Microsoft.Extensions.Logging;
using SC.Contract.Services.Notification;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.User.Enum;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.Services;

internal sealed class ServingFailureNotifier(
    IGenericRepository<UserAggregate, Guid> userRepository,
    IBusinessNotificationService businessNotificationService,
    ILogger<ServingFailureNotifier> logger) : IServingFailureNotifier
{
    public async Task NotifyStaffAsync(
        Guid orderId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var staff = await userRepository.FindListAsync(
            user => !user.IsDeleted && user.Role == Role.Staff,
            cancellationToken);

        if (staff.Count == 0)
        {
            logger.LogWarning(
                "ServingFailed for order {OrderId} but no Staff user to notify.",
                orderId);
            return;
        }

        var tokens = new Dictionary<string, string>
        {
            ["referenceId"] = orderId.ToString(),
            ["orderId"] = orderId.ToString(),
            ["reason"] = reason
        };

        foreach (var user in staff)
        {
            await businessNotificationService.NotifyAsync(
                NotificationTemplateKeys.ServingFailed,
                user.Id,
                orderId,
                tokens,
                new { OrderId = orderId, Reason = reason },
                cancellationToken);
        }
    }
}
