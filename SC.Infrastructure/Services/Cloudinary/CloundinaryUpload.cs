using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SC.Infrastructure.DependencyInjection.Options;

namespace SC.Infrastructure.Services.Cloudinary;

public sealed class CloundinaryUpload(
    CloudinaryDotNet.Cloudinary cloudinary,
    IOptions<CloundinaryOptions> options,
    ILogger<CloundinaryUpload> logger)
    : ICloundinaryUpload
{
    private readonly CloudinaryDotNet.Cloudinary _cloudinary = cloudinary;
    private readonly CloundinaryOptions _options = options.Value;
    private readonly ILogger<CloundinaryUpload> _logger = logger;

    public async Task<UploadResult> UploadFileAsync(Stream fileStream, string fileName)
    {
        if (fileStream.Length <= 0)
        {
            throw new ArgumentException("File is empty.", nameof(fileStream));
        }

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = _options.ImagesFolder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error is not null)
        {
            _logger.LogError("Cloudinary image upload error: {ErrorMessage}", uploadResult.Error.Message);
            throw new InvalidOperationException($"Upload failed: {uploadResult.Error.Message}");
        }

        return new UploadResult(uploadResult.Url?.ToString() ?? string.Empty,
            uploadResult.SecureUrl?.ToString() ?? string.Empty);
    }

    public async Task<UploadResult> UploadRawFileAsync(Stream fileStream, string fileName, string publicId)
    {
        if (fileStream.Length <= 0)
        {
            throw new ArgumentException("File is empty.", nameof(fileStream));
        }

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            PublicId = publicId,
            Overwrite = true
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error is not null)
        {
            _logger.LogError("Cloudinary raw upload error: {ErrorMessage}", uploadResult.Error.Message);
            throw new InvalidOperationException($"Upload failed: {uploadResult.Error.Message}");
        }

        return new UploadResult(uploadResult.Url?.ToString() ?? string.Empty,
            uploadResult.SecureUrl?.ToString() ?? string.Empty);
    }

    public async Task<bool> DeleteFile(string fileName)
    {
        var deleteParams = new DeletionParams(fileName)
        {
            ResourceType = ResourceType.Image
        };

        var deleteResult = await _cloudinary.DestroyAsync(deleteParams);
        if (deleteResult.Error is null)
        {
            return true;
        }

        _logger.LogWarning("Cloudinary image delete failed. Trying raw resource for {FileName}.", fileName);

        deleteParams.ResourceType = ResourceType.Raw;
        deleteResult = await _cloudinary.DestroyAsync(deleteParams);

        if (deleteResult.Error is not null)
        {
            _logger.LogError("Cloudinary delete error for {FileName}: {ErrorMessage}", fileName,
                deleteResult.Error.Message);
            return false;
        }

        return true;
    }

    public Task<string> GetPublicId(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Task.FromResult(string.Empty);
        }

        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
        {
            return Task.FromResult(string.Empty);
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var uploadIndex = Array.IndexOf(segments, "upload");
        if (uploadIndex < 0 || uploadIndex + 1 >= segments.Length)
        {
            return Task.FromResult(string.Empty);
        }

        var startIndex = uploadIndex + 1;
        if (startIndex < segments.Length
            && segments[startIndex].StartsWith("v", StringComparison.OrdinalIgnoreCase)
            && segments[startIndex][1..].All(char.IsDigit))
        {
            startIndex++;
        }

        var publicIdWithExtension = string.Join('/', segments[startIndex..]);
        var lastDot = publicIdWithExtension.LastIndexOf('.');
        var publicId = lastDot > 0
            ? publicIdWithExtension[..lastDot]
            : publicIdWithExtension;

        return Task.FromResult(publicId);
    }

}