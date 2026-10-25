using Brouj.Application.Abstractions.Storage;
using Brouj.Application.Common.DTOs;
namespace Brouj.Application.Common.Services;

public sealed class FileUpload
{
    private readonly IFileStorageService _storage;

    public FileUpload(IFileStorageService storage)
    {
        _storage = storage;
    }

    public Task<string> SaveAsync(UploadInput input, CancellationToken cancellationToken) =>
        _storage.SaveAsync(input.Content, input.FileName, input.ContentType, input.Length, cancellationToken);

    public async Task RemoveFailedAsync(string path)
    {
        // Cleanup is best effort and must preserve the original persistence error.
        try
        {
            await _storage.DeleteAsync(path, CancellationToken.None);
        }
        catch
        {
            // Filesystem recovery is an operational concern; never expose an internal path.
        }
    }
}
