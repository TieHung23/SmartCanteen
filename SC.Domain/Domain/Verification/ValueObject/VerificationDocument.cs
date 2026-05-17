using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Domain.Verification.Enum;

namespace SC.Domain.Domain.Verification.ValueObject;

public class VerificationDocument : Abstraction.Aggregates.ValueObject
{
    private VerificationDocument()
    {
    }

    public Guid Id { get; init; }
    public DocumentType DocumentType { get; init; }
    public string CloudinaryUrl { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public long FileSize { get; init; }
    public string MimeType { get; init; } = null!;
    public DateTimeOffset UploadedAt { get; init; }

    public Guid VerificationRequestId { get; set; }

    public static VerificationDocument Create(
        DocumentType type,
        string cloudinaryUrl,
        string fileName,
        long fileSize,
        string mimeType)
    {
        if (string.IsNullOrWhiteSpace(cloudinaryUrl))
            throw new ArgumentException("Document URL is required.", nameof(cloudinaryUrl));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (fileSize <= 0)
            throw new ArgumentException("File size must be positive.", nameof(fileSize));
        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException("MIME type is required.", nameof(mimeType));

        return new VerificationDocument
        {
            Id = Guid.NewGuid(),
            DocumentType = type,
            CloudinaryUrl = cloudinaryUrl,
            FileName = fileName,
            FileSize = fileSize,
            MimeType = mimeType,
            UploadedAt = DateTimeOffset.UtcNow
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
        yield return VerificationRequestId;
    }
}
