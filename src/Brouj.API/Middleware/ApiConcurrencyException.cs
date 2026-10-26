namespace Brouj.API.Middleware;

/// <summary>A sanitized transport marker; it deliberately retains no provider exception.</summary>
public sealed class ApiConcurrencyException : Exception
{
    public ApiConcurrencyException() : base("The resource changed during the operation.")
    {
    }
}
