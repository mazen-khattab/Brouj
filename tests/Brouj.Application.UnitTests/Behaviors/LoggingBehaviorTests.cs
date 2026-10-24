using Brouj.Application.Behaviors;
using Microsoft.Extensions.Logging;

namespace Brouj.Application.UnitTests.Behaviors;

public sealed class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_LogsRequestTypeWithoutReadingOrLoggingRequestContents()
    {
        const string secret = "secret-password-value";
        const string privateResponse = "synthetic-refresh-token-response";
        var logger = new RecordingLogger<LoggingBehavior<SensitiveRequest, string>>();
        var behavior = new LoggingBehavior<SensitiveRequest, string>(logger);
        var request = new SensitiveRequest(secret);

        var response = await behavior.Handle(
            request,
            cancellationToken => Task.FromResult(privateResponse),
            CancellationToken.None);

        Assert.Equal(privateResponse, response);
        Assert.False(request.WasStringified);
        Assert.Equal(2, logger.Messages.Count);
        Assert.All(logger.Messages, message =>
        {
            Assert.Contains(nameof(SensitiveRequest), message);
            Assert.DoesNotContain(secret, message, StringComparison.Ordinal);
            Assert.DoesNotContain(privateResponse, message, StringComparison.Ordinal);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_DoesNotLogSecretsFromFailuresOrCancellation(bool cancelled)
    {
        const string secret = "synthetic-password-token-cookie-identity\r\nforged-log-entry";
        using var cancellation = new CancellationTokenSource();

        if (cancelled)
        {
            cancellation.Cancel();
        }

        var request = new SensitiveRequest(secret);
        var logger = new RecordingLogger<LoggingBehavior<SensitiveRequest, string>>();
        var behavior = new LoggingBehavior<SensitiveRequest, string>(logger);
        Exception failure = cancelled
            ? new OperationCanceledException(secret, cancellation.Token)
            : new InvalidOperationException(secret);

        // A downstream exception may contain provider details. Propagate it to the caller while
        // keeping both structured log values and exception attachments free of those details.
        var thrown = await Assert.ThrowsAnyAsync<Exception>(() => behavior.Handle(
            request,
            token => Task.FromException<string>(failure),
            cancellation.Token));

        Assert.Same(failure, thrown);
        Assert.False(request.WasStringified);
        Assert.Equal(2, logger.Messages.Count);
        Assert.All(logger.Messages, message => Assert.DoesNotContain(secret, message));
        Assert.All(logger.Exceptions, exception => Assert.Null(exception));
        Assert.All(logger.Values, value => Assert.DoesNotContain(secret, value));
    }

    private sealed class SensitiveRequest(string password)
    {
        public string Password { get; } = password;
        public bool WasStringified { get; private set; }

        public override string ToString()
        {
            WasStringified = true;
            return Password;
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public List<Exception?> Exceptions { get; } = [];
        public List<string> Values { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            Exceptions.Add(exception);

            if (state is IEnumerable<KeyValuePair<string, object?>> values)
            {
                Values.AddRange(values.Select(value => value.Value?.ToString() ?? string.Empty));
            }
        }
    }
}
