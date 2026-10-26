using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Brouj.API.IntegrationTests.Support;

internal sealed class ApiTestLogger : ILogger
{
    private readonly ConcurrentQueue<string> _messages;

    public ApiTestLogger(ConcurrentQueue<string> messages)
    {
        _messages = messages;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _messages.Enqueue(formatter(state, exception) + exception?.ToString());
    }
}
