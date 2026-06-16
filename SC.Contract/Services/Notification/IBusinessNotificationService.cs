namespace SC.Contract.Services.Notification;

public interface IBusinessNotificationService
{
    Task NotifyAsync(
        string templateKey,
        Guid recipientId,
        Guid? referenceId,
        IReadOnlyDictionary<string, string> tokens,
        object? data = null,
        CancellationToken cancellationToken = default);
}
