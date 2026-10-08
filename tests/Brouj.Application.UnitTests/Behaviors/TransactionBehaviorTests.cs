using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Behaviors;

namespace Brouj.Application.UnitTests.Behaviors;

public sealed class TransactionBehaviorTests
{
    [Fact]
    public async Task Handle_WhenRequestIsTransactional_ExecutesThroughTransactionManager()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var expectedToken = cancellationTokenSource.Token;
        var handlerToken = CancellationToken.None;
        var transactionManager = new RecordingTransactionManager();
        var behavior = new TransactionBehavior<TransactionalRequest, string>(transactionManager);

        var response = await behavior.Handle(
            new TransactionalRequest(),
            cancellationToken =>
            {
                handlerToken = cancellationToken;
                return Task.FromResult("handled");
            },
            expectedToken);

        Assert.Equal("handled", response);
        Assert.Equal(1, transactionManager.ExecutionCount);
        Assert.Equal(expectedToken, transactionManager.CancellationToken);
        Assert.Equal(expectedToken, handlerToken);
    }

    [Fact]
    public async Task Handle_WhenRequestIsNotTransactional_BypassesTransactionManager()
    {
        var transactionManager = new RecordingTransactionManager();
        var behavior = new TransactionBehavior<NonTransactionalRequest, string>(transactionManager);

        var response = await behavior.Handle(
            new NonTransactionalRequest(),
            cancellationToken => Task.FromResult("handled"),
            CancellationToken.None);

        Assert.Equal("handled", response);
        Assert.Equal(0, transactionManager.ExecutionCount);
    }

    private sealed record TransactionalRequest : ITransactionalRequest;

    private sealed record NonTransactionalRequest;

    private sealed class RecordingTransactionManager : IApplicationTransactionManager
    {
        public int ExecutionCount { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            CancellationToken = cancellationToken;
            return operation(cancellationToken);
        }
    }
}
