using Brouj.Application.Abstractions.Storage;
using Brouj.Infrastructure.Persistence;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

internal sealed class Part9FileStorage(ApplicationDbContext context) : IFileStorageService
{
    public List<string> Deleted { get; } = [];

    public bool DeletedInsideTransaction { get; private set; }

    public Task<string> SaveAsync(
        Stream content, string originalFileName, string contentType, long length, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task DeleteAsync(string storedPath, CancellationToken cancellationToken)
    {
        DeletedInsideTransaction |= context.Database.CurrentTransaction is not null;
        Deleted.Add(storedPath);

        return Task.CompletedTask;
    }
}
