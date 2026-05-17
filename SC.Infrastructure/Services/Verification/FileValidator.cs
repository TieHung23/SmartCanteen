using Microsoft.Extensions.Options;
using SC.Contract.Services.Verification;
using SC.Contract.Shared;
using SC.Infrastructure.DependencyInjection.Options;

namespace SC.Infrastructure.Services.Verification;

public sealed class FileValidator : IFileValidator
{
    private readonly VerificationOptions _options;

    public FileValidator(IOptions<VerificationOptions> options)
    {
        _options = options.Value;
    }

    public Result Validate(string fileName, long fileSize, string mimeType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result.Failure(Error.EmptyValue, "File name is required.");

        if (fileSize <= 0)
            return Result.Failure(Error.EmptyValue, "File is empty.");

        if (fileSize > _options.FileSizeMaxBytes)
            return Result.Failure(Error.FileTooLarge, $"File exceeds {_options.FileSizeMaxBytes} bytes.");

        var normalized = mimeType?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!_options.AcceptedFormats.Any(f => f.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure(Error.UnsupportedFileFormat, $"MIME type '{mimeType}' is not allowed.");

        return Result.Success(null);
    }
}
