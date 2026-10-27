using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Brouj.Infrastructure.Persistence;

/// <summary>Owns the scoped context's transaction and translates expected conflicts after rollback.</summary>
public sealed class ApplicationTransactionManager : IApplicationTransactionManager
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApplicationTransactionManager> _logger;

    public ApplicationTransactionManager(ApplicationDbContext context, ILogger<ApplicationTransactionManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Nested commands participate in their caller's transaction and leave ownership to that caller.
        if (_context.Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        // A handler may have non-replayable effects or an uncertain commit. Never blindly replay it.
        var strategy = new NonReplayingExecutionStrategy(_context);

        try
        {
            return await strategy.ExecuteAsync(async ct =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(ct);

                _logger.LogDebug("Started application database transaction.");

                try
                {
                    var result = await operation(ct);

                    if (result is Result { IsFailure: true })
                    {
                        // A business rejection must never commit earlier writes from the same operation.
                        await transaction.RollbackAsync(CancellationToken.None);
                        _context.ChangeTracker.Clear();
                        _logger.LogDebug("Rolled back application transaction after a business rejection.");

                        return result;
                    }

                    await transaction.CommitAsync(ct);
                    _logger.LogDebug("Committed application database transaction.");

                    return result;
                }
                catch
                {
                    // DisposeAsync rolls back before the outer catch maps an expected conflict.
                    // Do not log provider exceptions: their messages may contain private row values.
                    _context.ChangeTracker.Clear();
                    _logger.LogWarning("Application transaction failed; uncommitted work will be rolled back.");

                    throw;
                }
            }, cancellationToken);
        }
        catch (Exception exception) when (ClassifyConflict(exception) is not null)
        {
            var conflict = ClassifyConflict(exception)!.Value;

            _logger.LogInformation("Database transaction rejected a {Conflict} conflict.", conflict);

            throw new PersistenceConflictException(conflict);
        }
    }

    private static PersistenceConflict? ClassifyConflict(Exception exception)
    {

        if (exception is DbUpdateConcurrencyException)
        {
            return PersistenceConflict.ConcurrentWrite;
        }

        var sql = exception as SqlException ?? exception.InnerException as SqlException;

        if (sql is null)
        {
            return null;
        }

        foreach (SqlError error in sql.Errors)
        {

            if (error.Number is 1205 or 3960)
            {
                return PersistenceConflict.ConcurrentWrite;
            }

            if (error.Number is not (2601 or 2627))
            {
                continue;
            }

            // Match only approved constraints. An unrelated unique failure is not evidence of a
            // duplicate reservation/order and must retain the generic failure path.
            if (error.Message.Contains("'UQ_Reservations_CustomerId_UnitId_Processing'", StringComparison.Ordinal))
            {
                return PersistenceConflict.ActiveReservation;
            }

            if (error.Message.Contains("'UQ_Orders_ReservationId'", StringComparison.Ordinal))
            {
                return PersistenceConflict.ReservationOrder;
            }
        }

        return null;
    }
}
