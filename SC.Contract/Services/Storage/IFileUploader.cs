namespace SC.Contract.Services.Storage;

public record FileUploadResult(string Url);

public interface IFileUploader
{
    Task<FileUploadResult> UploadAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);
}
