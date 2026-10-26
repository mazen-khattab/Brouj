using Brouj.Application.Abstractions.Storage;

namespace Brouj.Application.UnitTests.Features.Support;

internal sealed class FeatureFiles : IFileStorageService
{
    public string StoredPath { get; } = Guid.NewGuid().ToString("N") + ".png";

    public List<string> Deleted { get; } = [];

    public CancellationToken CleanupToken { get; private set; }

    public Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long length,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(StoredPath);
    }

    public Task DeleteAsync(string storedPath, CancellationToken cancellationToken)
    {
        CleanupToken = cancellationToken;
        Deleted.Add(storedPath);

        return Task.CompletedTask;
    }
}
