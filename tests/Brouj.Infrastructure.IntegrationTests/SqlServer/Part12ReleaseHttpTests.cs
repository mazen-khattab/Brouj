using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.IntegrationTests.Performance;
using Microsoft.Extensions.Logging;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

/// <summary>Runs critical browser/API flows through real HTTPS, real handlers and migrated SQL Server.</summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class Part12ReleaseHttpTests
{
    [SqlServerFact]
    public async Task Migrated_release_supports_customer_sessions_reservations_orders_ownership_and_staff_audit()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var cancellationToken = timeout.Token;
        using var logging = LoggerFactory.Create(builder => builder.AddSimpleConsole());
        var logger = logging.CreateLogger<Part12ReleaseHttpTests>();
        var root = FindRepositoryRoot();
        var output = Path.Combine(root, "artifacts", "verification", "part12-" + Guid.NewGuid().ToString("N"));
        await using var environment = new BaselineEnvironment(logging.CreateLogger<BaselineEnvironment>());

        // Reuse the already verified real-process fixture; this is a functional test, not another load run.
        await environment.StartAsync(root, output, cancellationToken, applyMigrations: true);
        using var customer = environment.NewClient();
        using var staff = environment.NewClient();
        using var foreignCustomer = environment.NewClient();

        // This functional gate permits cold-start EF/password initialization. Benchmark timeouts stay unchanged.
        customer.Timeout = TimeSpan.FromSeconds(30);
        staff.Timeout = TimeSpan.FromSeconds(30);
        foreignCustomer.Timeout = TimeSpan.FromSeconds(30);

        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)) + "aA1!";
        var email = "release-" + Guid.NewGuid().ToString("N") + "@example.test";
        var registration = new
        {
            FName = "Release",
            LName = "Verification",
            Email = email,
            Password = password,
            Phone = "010" + RandomNumberGenerator.GetInt32(10000000, 99999999),
            IdentityNumber = "3990101" + RandomNumberGenerator.GetInt32(1000000, 9999999)
        };

        logger.LogInformation("Verifying real customer registration, cookies and CSRF on the migrated release");
        await SendAsync(customer, HttpMethod.Get, "/api/v1/customers/me", HttpStatusCode.Unauthorized,
            cancellationToken);
        var session = await SendAsync(customer, HttpMethod.Post, "/api/v1/auth/customers/register",
            HttpStatusCode.Created, cancellationToken, registration);
        var customerId = session.GetProperty("actorId").GetGuid();
        var profile = await SendAsync(customer, HttpMethod.Get, "/api/v1/customers/me", HttpStatusCode.OK,
            cancellationToken);

        Assert.Equal(customerId, profile.GetProperty("id").GetGuid());

        var reservation = await SendAsync(customer, HttpMethod.Post, "/api/v1/reservations",
            HttpStatusCode.Created, cancellationToken, new { unitId = environment.Units[0].Unit });
        var reservationId = reservation.GetGuid();
        await SendAsync(customer, HttpMethod.Post, "/api/v1/reservations", HttpStatusCode.Conflict,
            cancellationToken, new { unitId = environment.Units[0].Unit });
        var owned = await SendAsync(customer, HttpMethod.Get, $"/api/v1/reservations/{reservationId}",
            HttpStatusCode.OK, cancellationToken);

        Assert.Equal(reservationId, owned.GetProperty("id").GetGuid());

        await SendAsync(foreignCustomer, HttpMethod.Get, $"/api/v1/reservations/{reservationId}",
            HttpStatusCode.NotFound, cancellationToken, bearer: environment.Sessions[0].Credentials.Token);

        logger.LogInformation("Verifying persisted reservation/order transitions and cross-customer denial");
        var order = await SendAsync(staff, HttpMethod.Post, $"/api/v1/admin/reservations/{reservationId}/order",
            HttpStatusCode.Created, cancellationToken, bearer: environment.Staff.Credentials.Token);
        var orderId = order.GetGuid();
        await SendAsync(staff, HttpMethod.Post, $"/api/v1/admin/reservations/{reservationId}/order",
            HttpStatusCode.Conflict, cancellationToken, bearer: environment.Staff.Credentials.Token);
        var orderDetails = await SendAsync(customer, HttpMethod.Get, $"/api/v1/admin/orders/{orderId}",
            HttpStatusCode.OK, cancellationToken);

        // The fixture unit is 100 square meters at 100 per meter; the HTTP caller supplies neither total nor status.
        Assert.Equal(10000m, orderDetails.GetProperty("totalPrice").GetDecimal());
        Assert.Equal((int)OrderStatus.Processing, orderDetails.GetProperty("status").GetInt32());

        await SendAsync(foreignCustomer, HttpMethod.Get, $"/api/v1/admin/orders/{orderId}", HttpStatusCode.NotFound,
            cancellationToken, bearer: environment.Sessions[0].Credentials.Token);
        await SendAsync(customer, HttpMethod.Post, $"/api/v1/reservations/{reservationId}/cancel",
            HttpStatusCode.Conflict, cancellationToken);
        var confirmed = await SendAsync(customer, HttpMethod.Get, $"/api/v1/reservations/{reservationId}",
            HttpStatusCode.OK, cancellationToken);

        Assert.Equal((int)ReservationStatus.Confirmed, confirmed.GetProperty("status").GetInt32());

        var cancellable = await SendAsync(customer, HttpMethod.Post, "/api/v1/reservations", HttpStatusCode.Created,
            cancellationToken, new { unitId = environment.Units[1].Unit });
        var cancelledId = cancellable.GetGuid();
        await SendAsync(customer, HttpMethod.Post, $"/api/v1/reservations/{cancelledId}/cancel", HttpStatusCode.OK,
            cancellationToken);
        var cancelled = await SendAsync(customer, HttpMethod.Get, $"/api/v1/reservations/{cancelledId}",
            HttpStatusCode.OK, cancellationToken);

        Assert.Equal((int)ReservationStatus.Cancelled, cancelled.GetProperty("status").GetInt32());

        var city = await SendAsync(staff, HttpMethod.Post, "/api/v1/admin/cities", HttpStatusCode.Created,
            cancellationToken, new { name = "Final verification city" }, environment.Staff.Credentials.Token);
        var cityId = city.GetGuid();
        var publicCity = await SendAsync(customer, HttpMethod.Get, $"/api/v1/cities/{cityId}", HttpStatusCode.OK,
            cancellationToken);

        Assert.Equal("Final verification city", publicCity.GetProperty("name").GetString());

        await using (var connection = await environment.OpenDatabaseAsync(cancellationToken))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM ActivityLogs WHERE UserId = @actor AND EntityId = @entity";
            command.Parameters.AddWithValue("@actor", environment.Admin);
            command.Parameters.AddWithValue("@entity", cityId);

            Assert.Equal(1, (int)(await command.ExecuteScalarAsync(cancellationToken))!);
        }

        logger.LogInformation("Verifying real refresh rotation, logout invalidation and password login");
        var beforeRefresh = await ActiveRefreshIdAsync(environment, customerId, cancellationToken);
        await SendAsync(customer, HttpMethod.Post, "/api/v1/auth/customers/refresh", HttpStatusCode.OK,
            cancellationToken);
        var afterRefresh = await ActiveRefreshIdAsync(environment, customerId, cancellationToken);

        Assert.NotNull(beforeRefresh);
        Assert.NotNull(afterRefresh);
        Assert.NotEqual(beforeRefresh, afterRefresh);

        await SendAsync(customer, HttpMethod.Post, "/api/v1/auth/customers/logout", HttpStatusCode.OK,
            cancellationToken);
        await SendAsync(customer, HttpMethod.Get, "/api/v1/customers/me", HttpStatusCode.Unauthorized,
            cancellationToken);

        Assert.Null(await ActiveRefreshIdAsync(environment, customerId, cancellationToken));

        var login = await SendAsync(customer, HttpMethod.Post, "/api/v1/auth/customers/login", HttpStatusCode.OK,
            cancellationToken, new { email, password });

        Assert.Equal(customerId, login.GetProperty("actorId").GetGuid());

        await SendAsync(customer, HttpMethod.Get, "/api/v1/customers/me", HttpStatusCode.OK, cancellationToken);
        await SendAsync(customer, HttpMethod.Post, "/api/v1/auth/customers/logout", HttpStatusCode.OK,
            cancellationToken);

        Assert.Null(await ActiveRefreshIdAsync(environment, customerId, cancellationToken));
        logger.LogInformation("Critical migrated HTTPS/SQL release flow passed without persistence/session doubles");
    }

    private static async Task<JsonElement> SendAsync(HttpClient client, HttpMethod method, string path,
        HttpStatusCode expected, CancellationToken cancellationToken, object? body = null, string? bearer = null)
    {
        using var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (bearer is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        if (method != HttpMethod.Get)
        {
            using var proofRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/csrf");

            if (bearer is not null)
            {
                proofRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            }

            using var proofResponse = await client.SendAsync(proofRequest, cancellationToken);
            Assert.True(proofResponse.IsSuccessStatusCode, "CSRF bootstrap failed during release verification.");
            using var proof = JsonDocument.Parse(await proofResponse.Content.ReadAsStringAsync(cancellationToken));
            request.Headers.Add("X-CSRF-TOKEN", proof.RootElement.GetProperty("data")
                .GetProperty("requestToken").GetString());
        }

        using var response = await client.SendAsync(request, cancellationToken);

        // Report transport metadata only: response bodies can include identity data and cookies contain credentials.
        Assert.True(response.StatusCode == expected,
            $"Release HTTP {method} {path}: expected {(int)expected}, received {(int)response.StatusCode}.");

        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        return json.RootElement.GetProperty("data").Clone();
    }

    private static async Task<Guid?> ActiveRefreshIdAsync(BaselineEnvironment environment, Guid customerId,
        CancellationToken cancellationToken)
    {
        await using var connection = await environment.OpenDatabaseAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM CustomerRefreshTokens WHERE CustomerId = @id AND IsActive = 1";
        command.Parameters.AddWithValue("@id", customerId);
        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is Guid id ? id : null;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Brouj.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Brouj repository root was not found.");
    }
}
