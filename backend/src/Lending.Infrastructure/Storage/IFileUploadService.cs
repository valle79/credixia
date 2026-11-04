namespace Lending.Infrastructure.Storage;

public interface IFileUploadService
{
    Task<string> UploadImageAsync(Stream imageStream, string fileName, string folder, CancellationToken cancellationToken = default);
    Task<bool> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default);
}
