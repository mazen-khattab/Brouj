namespace Brouj.Application.Abstractions.Persistence;

/// <summary>Carries a safe conflict category after the database transaction has rolled back.</summary>
public sealed class PersistenceConflictException : Exception
{
    public PersistenceConflictException(PersistenceConflict conflict) : base("A database write conflict occurred.")
    {
        Conflict = conflict;
    }

    public PersistenceConflict Conflict { get; }
}
