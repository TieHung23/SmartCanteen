using SC.Contract.Shared;

namespace SC.Contract.Services.Verification;

public interface IFileValidator
{
    Result Validate(string fileName, long fileSize, string mimeType);
}
