using Microsoft.Extensions.Logging;
using SC.Contract.Services.Notification;

namespace SC.Application.Services;

internal sealed class BusinessNotificationService(
    INotificationService notificationService,
    INotificationTemplateProvider templateProvider,
    ILogger<BusinessNotificationService> logger)
    : IBusinessNotificationService
{
    public async Task NotifyAsync(
        string templateKey,
        Guid recipientId,
        Guid? referenceId,
        IReadOnlyDictionary<string, string> tokens,
        object? data = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = templateProvider.Render(templateKey, tokens);
            if (template is null)
            {
                logger.LogWarning(
                    "Notification template {TemplateKey} could not be rendered",
                    templateKey);
                return;
            }

            await notificationService.SendAsync(
                new NotificationRequest(
                    recipientId,
                    template.Type,
                    template.Title,
                    template.MessageTemplate,
                    template.ReferenceType,
                    referenceId,
                    template.ActionUrlTemplate,
                    data),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Business notification {TemplateKey} could not be sent to {RecipientId}",
                templateKey,
                recipientId);
        }
    }
}
