using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Brouj.API.IntegrationTests.Support;

internal sealed class ListDbSet<T> : DbSet<T>, IQueryable<T>, IAsyncEnumerable<T> where T : class
{
    private readonly List<T> _items = [];
    private readonly Action? _onExecute;

    public ListDbSet(Action? onExecute = null)
    {
        _onExecute = onExecute;
    }

    public override IEntityType EntityType => throw new NotSupportedException();

    Type IQueryable.ElementType => typeof(T);

    Expression IQueryable.Expression => _items.AsQueryable().Expression;

    IQueryProvider IQueryable.Provider => new ListAsyncProvider(_items.AsQueryable().Provider, _onExecute);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    public override EntityEntry<T> Add(T entity)
    {
        _items.Add(entity);

        return null!;
    }

    public override EntityEntry<T> Remove(T entity)
    {
        _items.Remove(entity);

        return null!;
    }

    public override void AddRange(IEnumerable<T> entities) => _items.AddRange(entities);

    public override void AddRange(params T[] entities) => _items.AddRange(entities);

    public override void RemoveRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities.ToArray())
        {
            _items.Remove(entity);
        }
    }

    public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new ListAsyncEnumerator<T>(_items.GetEnumerator(), cancellationToken);
}
