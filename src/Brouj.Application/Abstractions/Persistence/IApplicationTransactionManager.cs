namespace Brouj.Application.Abstractions.Persistence;

public interface IApplicationTransactionManager
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);
}
