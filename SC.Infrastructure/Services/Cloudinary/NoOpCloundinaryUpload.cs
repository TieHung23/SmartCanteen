using Microsoft.Extensions.Logging;

namespace SC.Infrastructure.Services.Cloudinary;

public sealed class NoOpCloundinaryUpload(ILogger<NoOpCloundinaryUpload> logger) : ICloundinaryUpload
{
    private readonly ILogger<NoOpCloundinaryUpload> _logger = logger;

    public Task<UploadResult> UploadFileAsync(Stream fileStream, string fileName)
    {
        _logger.LogWarning("Cloudinary is not configured. Image upload skipped for {FileName}.", fileName);
        return Task.FromResult(new UploadResult(string.Empty, string.Empty));
    }

    public Task<UploadResult> UploadRawFileAsync(Stream fileStream, string fileName, string publicId)
    {
        _logger.LogWarning("Cloudinary is not configured. Raw upload skipped for {FileName}.", fileName);
        return Task.FromResult(new UploadResult(string.Empty, string.Empty));
    }

    public Task<bool> DeleteFile(string fileName)
    {
        _logger.LogWarning("Cloudinary is not configured. Delete skipped for {FileName}.", fileName);
        return Task.FromResult(false);
    }

    public Task<string> GetPublicId(string fileUrl)
    {
        return Task.FromResult(string.Empty);
    }
}
