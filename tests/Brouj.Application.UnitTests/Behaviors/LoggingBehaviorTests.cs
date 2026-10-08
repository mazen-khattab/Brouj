using Brouj.Application.Behaviors;
using Microsoft.Extensions.Logging;

namespace Brouj.Application.UnitTests.Behaviors;

public sealed class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_LogsRequestTypeWithoutReadingOrLoggingRequestContents()
    {
        const string secret = "secret-password-value";
        var logger = new RecordingLogger<LoggingBehavior<SensitiveRequest, string>>();
        var behavior = new LoggingBehavior<SensitiveRequest, string>(logger);
        var request = new SensitiveRequest(secret);

        var response = await behavior.Handle(
            request,
            cancellationToken => Task.FromResult("handled"),
            CancellationToken.None);

        Assert.Equal("handled", response);
        Assert.False(request.WasStringified);
        Assert.Equal(2, logger.Messages.Count);
        Assert.All(logger.Messages, message =>
        {
            Assert.Contains(nameof(SensitiveRequest), message);
            Assert.DoesNotContain(secret, message, StringComparison.Ordinal);
        });
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
        }
    }
}
