using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Brouj.API.IntegrationTests.Support;

internal sealed class ApiTestLogProvider : ILoggerProvider
{
    public ConcurrentQueue<string> Messages { get; } = new();

    public ILogger CreateLogger(string categoryName) => new ApiTestLogger(Messages);

    public void Dispose()
    {
    }
}
