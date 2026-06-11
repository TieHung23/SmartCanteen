using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.Refund.Entity;

public class RefundRequestImage : Entity<Guid>, IAuditableEntity<Guid>
{
    private RefundRequestImage()
    {
    }

    public Guid RefundRequestId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static RefundRequestImage Create(
        string imageUrl,
        string fileName,
        Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("Image URL is required.", nameof(imageUrl));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("Created by is required.", nameof(createdBy));
        }

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
}
