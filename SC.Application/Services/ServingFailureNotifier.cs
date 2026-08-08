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
    public Task NotifyStaffAsync(
        Guid orderId,
        string reason,
        CancellationToken cancellationToken = default)
        => NotifyAllStaffAsync(
            NotificationTemplateKeys.ServingFailed,
            orderId,
            new Dictionary<string, string>
            {
                ["referenceId"] = orderId.ToString(),
                ["orderId"] = orderId.ToString(),
                ["reason"] = reason
            },
            new { OrderId = orderId, Reason = reason },
            cancellationToken);

    public async Task NotifyAllStaffAsync(
        string templateKey,
        Guid orderId,
        IReadOnlyDictionary<string, string> tokens,
        object? data = null,
        CancellationToken cancellationToken = default)
    {
        var staff = await userRepository.FindListAsync(
            user => !user.IsDeleted && user.Role == Role.Staff,
            cancellationToken);

        if (staff.Count == 0)
        {
            logger.LogWarning(
                "Staff notification '{Template}' for order {OrderId} but no Staff user to notify.",
                templateKey, orderId);
            return;
        }

        foreach (var user in staff)
        {
            await businessNotificationService.NotifyAsync(
                templateKey, user.Id, orderId, tokens, data, cancellationToken);
        }
    }
}
