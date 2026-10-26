using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Brouj.Application.UnitTests.Features.Support;

internal sealed class ListAsyncProvider : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;
    private readonly Action? _onExecute;

    public ListAsyncProvider(IQueryProvider inner, Action? onExecute = null)
    {
        _inner = inner;
        _onExecute = onExecute;
    }

    public IQueryable CreateQuery(Expression expression) =>
        (IQueryable)Activator.CreateInstance(
            typeof(ListAsyncQuery<>).MakeGenericType(expression.Type.GetGenericArguments()[0]), expression, _onExecute)!;

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new ListAsyncQuery<TElement>(expression, _onExecute);

    public object? Execute(Expression expression) => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _onExecute?.Invoke();

        var result = _inner.Execute(expression);
        var resultType = typeof(TResult).GetGenericArguments()[0];

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType).Invoke(null, [result])!;
    }
}
