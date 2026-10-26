using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;

namespace Brouj.API.IntegrationTests.Support;

internal static class ApiTestConfiguration
{
    public static Dictionary<string, string?> Create()
    {
        var values = new Dictionary<string, string?>
        {
            [HostDefaults.EnvironmentKey] = Environments.Production,
            ["ConnectionStrings:DefaultConnection"] =
                "Server=localhost;Database=UnusedPart8Tests;Integrated Security=True;TrustServerCertificate=True;",
            ["Jwt:Issuer"] = "brouj-api-tests",
            ["Jwt:Audience"] = "brouj-api-test-client",
            ["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ["FileStorage:RootPath"] = Path.Combine(Path.GetTempPath(), "BroujPart8Tests", Guid.NewGuid().ToString("N")),
            ["Caching:Provider"] = "Memory",
            ["AllowedHosts"] = "brouj.test",
            ["Api:BaseUrl"] = "https://brouj.test",
            ["Api:AllowedOrigins:0"] = "https://brouj.com",
            ["Api:JsonBodyLimitBytes"] = "16384",
            ["Api:MultipartBodyLimitBytes"] = "6291456",
            ["Api:MaximumJsonDepth"] = "16"
        };

        // Synthetic test budgets are intentionally absent from deployed application settings.
        foreach (var category in new[] { "PublicReads", "Authentication", "ReservationWrites", "AdminWrites", "FileUploads" })
        {
            values[$"RateLimits:{category}:PermitLimit"] = "10000";
            values[$"RateLimits:{category}:WindowSeconds"] = "60";
        }

        return values;
    }
}
