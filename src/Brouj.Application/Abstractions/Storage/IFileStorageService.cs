namespace Brouj.Application.Abstractions.Storage;

public interface IFileStorageService
{
    Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long length,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string storedPath,
        CancellationToken cancellationToken);
}
