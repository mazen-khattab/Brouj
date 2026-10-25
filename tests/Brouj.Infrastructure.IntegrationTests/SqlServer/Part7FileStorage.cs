using Brouj.Application.Abstractions.Storage;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

internal sealed class Part7FileStorage : IFileStorageService
{
    public Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long length,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task DeleteAsync(string storedPath, CancellationToken cancellationToken) => Task.CompletedTask;
}
