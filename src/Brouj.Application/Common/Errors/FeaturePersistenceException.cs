namespace Brouj.Application.Common.Errors;


public sealed class FeaturePersistenceException : Exception
{
    // Provider exceptions can embed identity values. Keep their message and inner chain out of host logs.
    public FeaturePersistenceException() : base("The application operation could not be persisted.")
    {
    }
}
