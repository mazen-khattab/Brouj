using Brouj.Application.Abstractions.Storage;
using Microsoft.Extensions.Logging;
namespace Brouj.Application.Common.Services;

public sealed class PendingFileCleanup
{
    private readonly IFileStorageService _storage;
    private readonly ILogger<PendingFileCleanup> _logger;
    private readonly HashSet<string> _paths = [];

    public PendingFileCleanup(IFileStorageService storage, ILogger<PendingFileCleanup> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public void Add(string path) => _paths.Add(path);

    public void Discard() => _paths.Clear();

    public async Task CompleteAsync()
    {
        foreach (var path in _paths)
        {
            try
            {
                // The database has committed. Client cancellation must not leave ordinary orphan files.
                await _storage.DeleteAsync(path, CancellationToken.None);
            }
            catch
            {
                // Do not log storage paths or turn a committed business write into a reported failure.
                _logger.LogWarning("A committed file removal requires a later cleanup attempt.");
            }
        }

        _paths.Clear();
    }
}
