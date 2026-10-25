namespace Brouj.Application.UnitTests.Features.Support;

internal sealed class ListAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    private readonly CancellationToken _cancellationToken;

    public ListAsyncEnumerator(IEnumerator<T> inner, CancellationToken cancellationToken)
    {
        _inner = inner;
        _cancellationToken = cancellationToken;
    }

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync()
    {
        _cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(_inner.MoveNext());
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();

        return ValueTask.CompletedTask;
    }
}
