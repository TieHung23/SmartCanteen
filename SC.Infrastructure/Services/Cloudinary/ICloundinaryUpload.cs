namespace SC.Infrastructure.Services.Cloudinary;

public interface ICloundinaryUpload
{
    Task<UploadResult> UploadFileAsync(Stream fileStream, string fileName);

    Task<UploadResult> UploadRawFileAsync(Stream fileStream, string fileName, string publicId);

    Task<bool> DeleteFile(string fileName);

    Task<string> GetPublicId(string fileUrl);
}

public record UploadResult(string Url, string ViewUrl);