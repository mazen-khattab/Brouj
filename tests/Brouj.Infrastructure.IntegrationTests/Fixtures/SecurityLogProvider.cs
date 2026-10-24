using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Brouj.Infrastructure.IntegrationTests.Fixtures;

/// <summary>Captures both rendered messages and structured values, including exception chains.</summary>
internal sealed class SecurityLogProvider : ILoggerProvider
{
    public ConcurrentQueue<string> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new RecordingLogger(this);
    }

    public void Dispose()
    {
    }

    private sealed class RecordingLogger(SecurityLogProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            provider.Entries.Enqueue(formatter(state, exception));

            if (state is IEnumerable<KeyValuePair<string, object?>> values)
            {
                foreach (var value in values)
                {
                    provider.Entries.Enqueue(value.Value?.ToString() ?? string.Empty);
                }
            }

            if (exception is not null)
            {
                provider.Entries.Enqueue(exception.ToString());
            }
        }
    }
}
