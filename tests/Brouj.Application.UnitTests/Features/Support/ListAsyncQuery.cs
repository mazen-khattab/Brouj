using System.Linq.Expressions;

namespace Brouj.Application.UnitTests.Features.Support;

internal sealed class ListAsyncQuery<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    private readonly Action? _onExecute;

    public ListAsyncQuery(Expression expression, Action? onExecute = null) : base(expression)
    {
        _onExecute = onExecute;
    }

    IQueryProvider IQueryable.Provider => new ListAsyncProvider(this, _onExecute);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new ListAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator(), cancellationToken);
}
