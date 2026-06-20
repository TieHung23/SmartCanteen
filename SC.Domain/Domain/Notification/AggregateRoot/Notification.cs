using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.SharedKernel;

namespace SC.Domain.Domain.Notification.AggregateRoot;

public sealed class Notification : AggregateRoot<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private Notification()
    {
    }

    public Guid RecipientId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? ActionUrl { get; private set; }
    public string? DataJson { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static Notification Create(
        Guid recipientId,
        string type,
        string title,
        string message,
        string? referenceType = null,
        Guid? referenceId = null,
        string? actionUrl = null,
        string? dataJson = null,
        Guid? createdBy = null)
    {
        if (recipientId == Guid.Empty)
        {
            throw new ArgumentException("Recipient ID is required.", nameof(recipientId));
        }

        ValidateRequired(type, nameof(type), NotificationConstraints.TypeMaxLength);
        ValidateRequired(title, nameof(title), NotificationConstraints.TitleMaxLength);
        ValidateRequired(message, nameof(message), NotificationConstraints.MessageMaxLength);
        ValidateOptional(referenceType, nameof(referenceType), NotificationConstraints.ReferenceTypeMaxLength);
        ValidateOptional(actionUrl, nameof(actionUrl), NotificationConstraints.ActionUrlMaxLength);
        ValidateOptional(dataJson, nameof(dataJson), NotificationConstraints.DataJsonMaxLength);

        if (referenceId.HasValue && string.IsNullOrWhiteSpace(referenceType))
        {
            throw new ArgumentException(
                "Reference type is required when reference ID is provided.",
                nameof(referenceType));
        }

        return new Notification
        {
            Id = Guid.NewGuid(),
            RecipientId = recipientId,
            Type = type.Trim(),
            Title = title.Trim(),
            Message = message.Trim(),
            ReferenceType = NormalizeOptional(referenceType),
            ReferenceId = referenceId,
            ActionUrl = NormalizeOptional(actionUrl),
            DataJson = NormalizeOptional(dataJson),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy ?? Guid.Empty,
            UpdatedBy = createdBy ?? Guid.Empty
        };
    }

    public void MarkAsRead(Guid updatedBy)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = ReadAtUtc;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static void ValidateRequired(
        string value,
        string parameterName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }
    }

    private static void ValidateOptional(
        string? value,
        string parameterName,
        int maxLength)
    {
        if (value?.Trim().Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
