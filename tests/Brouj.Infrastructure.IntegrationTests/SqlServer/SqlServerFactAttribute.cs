namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

internal static class SqlServerTestEnvironment
{
    public static bool Enabled =>
        Environment.GetEnvironmentVariable("BROUJ_RUN_SQLSERVER_TESTS") == "1" ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BROUJ_SQLSERVER_TEST_CONNECTION_STRING"));

    public const string PendingReason =
        "Pending SQL Server integration verification: set BROUJ_RUN_SQLSERVER_TESTS=1 with a compatible container runtime, or supply BROUJ_SQLSERVER_TEST_CONNECTION_STRING for a test server.";
}

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (!SqlServerTestEnvironment.Enabled)
        {
            Skip = SqlServerTestEnvironment.PendingReason;
        }
    }
}

public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute()
    {
        if (!SqlServerTestEnvironment.Enabled)
        {
            Skip = SqlServerTestEnvironment.PendingReason;
        }
    }
}
