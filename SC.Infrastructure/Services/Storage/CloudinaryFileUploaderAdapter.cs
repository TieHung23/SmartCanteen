using SC.Contract.Services.Storage;
using SC.Infrastructure.Services.Cloudinary;

namespace SC.Infrastructure.Services.Storage;

/// <summary>
/// Bridges the Application-layer IFileUploader abstraction onto the existing
/// ICloundinaryUpload implementation so handlers do not depend on Cloudinary
/// directly.
/// </summary>
public sealed class CloudinaryFileUploaderAdapter : IFileUploader
{
    private readonly ICloundinaryUpload _inner;

    public CloudinaryFileUploaderAdapter(ICloundinaryUpload inner)
    {
        _inner = inner;
    }

    public async Task<FileUploadResult> UploadAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.UploadFileAsync(content, fileName);
        var url = string.IsNullOrWhiteSpace(result.ViewUrl) ? result.Url : result.ViewUrl;
        return new FileUploadResult(url);
    }
}
