namespace Brouj.Application.Abstractions.Persistence;

/// <summary>Expected database conflicts without provider messages or private row values.</summary>
public enum PersistenceConflict
{
    ActiveReservation,
    ReservationOrder,
    ConcurrentWrite
}
