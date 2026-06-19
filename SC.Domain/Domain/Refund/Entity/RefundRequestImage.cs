using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.Refund.Entity;

public class RefundRequestImage : Entity<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private RefundRequestImage() { }

    public Guid RefundRequestId { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static RefundRequestImage Create(
        string imageUrl,
        string fileName,
        Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("Image URL is required.", nameof(imageUrl));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (createdBy == Guid.Empty)
            throw new ArgumentException("Created by is required.", nameof(createdBy));

        var now = DateTimeOffset.UtcNow;
        return new RefundRequestImage
        {
            Id = Guid.NewGuid(),
            ImageUrl = imageUrl.Trim(),
            FileName = fileName.Trim(),
            UploadedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }
}
