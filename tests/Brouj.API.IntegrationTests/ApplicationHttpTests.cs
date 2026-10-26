using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Brouj.API.IntegrationTests.Support;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.API.IntegrationTests;

/// <summary>Real HTTP and MediatR behaviors/handlers, with query and transaction doubles rather than SQL Server.</summary>
public sealed class ApplicationHttpTests
{
    [Fact]
    public async Task Ownership_filters_reject_read_and_cancel_IDOR_and_scope_customer_lists()
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var unit = new BaseUnit { Id = Guid.NewGuid(), Number = "A1", Project = new BaseProject { Name = "Project" } };
        var own = new Reservation
        {
            Id = Guid.NewGuid(), Unit = unit, UnitId = unit.Id, CustomerId = factory.CustomerId,
            Status = ReservationStatus.Processing, Number = "OWN"
        };
        var foreign = new Reservation
        {
            Id = Guid.NewGuid(), Unit = unit, UnitId = unit.Id, CustomerId = factory.OtherCustomerId,
            Status = ReservationStatus.Processing, Number = "FOREIGN"
        };
        factory.Database.Reservations.AddRange(own, foreign);
        factory.Database.Orders.AddRange(
            new Order { Id = Guid.NewGuid(), ReservationId = own.Id, Reservation = own, Number = "OWN-ORDER" },
            new Order { Id = Guid.NewGuid(), ReservationId = foreign.Id, Reservation = foreign, Number = "FOREIGN-ORDER" });
        var credential = factory.Credential("CustomerOnly");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credential);

        using var deniedRead = await client.GetAsync($"/api/v1/reservations/{foreign.Id}?customerId={factory.OtherCustomerId}");
        Assert.Equal(HttpStatusCode.NotFound, deniedRead.StatusCode);
        using var cancel = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{foreign.Id}/cancel");
        await HttpTestRequests.AddProofAsync(client, cancel, credential);
        using var deniedWrite = await client.SendAsync(cancel);
        Assert.Equal(HttpStatusCode.NotFound, deniedWrite.StatusCode);
        Assert.Equal(ReservationStatus.Processing, foreign.Status);
        Assert.Equal(0, factory.Database.SaveCount);

        foreach (var resource in new[] { "reservations", "orders" })
        {
            using var response = await client.GetAsync($"/api/v1/{resource}/me?customerId={factory.OtherCustomerId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("OWN", content);
            Assert.DoesNotContain("FOREIGN", content);
        }

        using var ownRead = await client.GetAsync($"/api/v1/reservations/{own.Id}");
        Assert.Equal(HttpStatusCode.OK, ownRead.StatusCode);
    }

    [Fact]
    public async Task Cancellation_maps_actual_business_conflicts_to_409()
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(), CustomerId = factory.CustomerId, Status = ReservationStatus.Cancelled
        };
        factory.Database.Reservations.Add(reservation);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{reservation.Id}/cancel");
        await HttpTestRequests.AddProofAsync(client, request, factory.Credential("CustomerOnly"));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(0, factory.Database.SaveCount);
    }

    [Theory]
    [InlineData("pageNumber=0")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=invalid")]
    public async Task Invalid_pagination_is_400(string query)
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync("/api/v1/cities?" + query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Pagination_and_query_filters_reach_the_real_handler()
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();

        for (var index = 1; index <= 5; index++)
        {
            factory.Database.Cities.Add(new City { Id = Guid.NewGuid(), Name = "City " + index });
        }

        using var response = await client.GetAsync("/api/v1/cities?pageNumber=2&pageSize=2&search=City");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(2, data.GetProperty("pageNumber").GetInt32());
        Assert.Equal(2, data.GetProperty("pageSize").GetInt32());
        Assert.Equal(5, data.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, data.GetProperty("items").GetArrayLength());
        Assert.Equal("City 3", data.GetProperty("items")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Unit_detail_requires_the_existing_project_scope_and_ignores_query_ID_overrides()
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var unit = new BaseUnit { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid(), Number = "Scoped unit" };
        factory.Database.BaseUnits.Add(unit);
        using var missingScope = await client.GetAsync($"/api/v1/units/{unit.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, missingScope.StatusCode);
        using var wrongScope = await client.GetAsync($"/api/v1/units/{unit.Id}?projectId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, wrongScope.StatusCode);
        using var valid = await client.GetAsync($"/api/v1/units/{unit.Id}?projectId={unit.ProjectId}&id={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        Assert.Contains("Scoped unit", await valid.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Staff_management_requires_SuperAdmin_and_account_state_remains_authoritative()
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Credential("AdminOnly"));
        using var admin = await client.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Credential("SuperAdminOnly"));
        using var superAdmin = await client.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, superAdmin.StatusCode);

        factory.Database.Users.Single(user => user.Id == factory.SuperAdminId).Role = UserRole.Admin;
        using var staleRole = await client.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, staleRole.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Credential("CustomerOnly"));
        factory.Database.Customers.Single(customer => customer.Id == factory.CustomerId).DeletedAt = DateTimeOffset.UtcNow;
        using var deleted = await client.GetAsync("/api/v1/customers/me");
        Assert.Equal(HttpStatusCode.Unauthorized, deleted.StatusCode);
    }

    [Theory]
    [InlineData("CustomerOnly")]
    [InlineData("StaffOnly")]
    public async Task Time_plan_read_preserves_approved_member_access(string actor)
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Credential(actor));

        using var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/time-plan");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(false, 500)]
    [InlineData(true, 409)]
    public async Task Provider_failures_are_sanitized_and_concurrency_is_mapped(bool concurrency, int status)
    {
        const string diagnostic = "SQL private_password=DO_NOT_LOG connection=secret";
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        factory.Database.SaveException = concurrency ? new DbUpdateConcurrencyException(diagnostic)
            : new DbUpdateException(diagnostic);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/cities")
        {
            Content = JsonContent.Create(new { Name = "Cairo" })
        };
        await HttpTestRequests.AddProofAsync(client, request, factory.Credential("AdminOnly"));

        using var response = await client.SendAsync(request);

        Assert.Equal(status, (int)response.StatusCode);
        Assert.DoesNotContain(diagnostic, await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(factory.Logs.Messages, message => message.Contains(diagnostic, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("customers")]
    [InlineData("users")]
    public async Task Login_refresh_replay_and_logout_use_real_handlers_and_secure_cookie_transport(string realm)
    {
        const string password = "Part8-Test-Password-123!";
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var hash = factory.Services.GetRequiredService<IPasswordHasher>().Hash(password);
        var email = realm == "customers" ? "customer@example.test" : "staff@example.test";

        if (realm == "customers")
        {
            factory.Database.Customers.Single(customer => customer.Id == factory.CustomerId).HashPassword = hash;
        }
        else
        {
            var staff = factory.Database.Users.Single(user => user.Id == factory.AdminId);
            staff.Email = email;
            staff.HashPassword = hash;
        }

        using var login = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/{realm}/login")
        {
            Content = JsonContent.Create(new { Email = email, Password = password })
        };
        await HttpTestRequests.AddProofAsync(client, login, "");
        using var loggedIn = await client.SendAsync(login);
        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
        var cookies = loggedIn.Headers.GetValues("Set-Cookie").ToArray();

        foreach (var cookie in cookies)
        {
            Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("path=/", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("domain=", cookie, StringComparison.OrdinalIgnoreCase);
        }

        var refreshCookie = cookies.Single(cookie => cookie.StartsWith("__Host-brouj-refresh=", StringComparison.Ordinal)).Split(';')[0];
        var accessCookie = cookies.Single(cookie => cookie.StartsWith("__Host-brouj-access=", StringComparison.Ordinal)).Split(';')[0];
        var body = await loggedIn.Content.ReadAsStringAsync();
        Assert.DoesNotContain("accessToken", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", body, StringComparison.OrdinalIgnoreCase);

        // An expired/missing access cookie can still refresh using an anonymous CSRF proof and its refresh cookie.
        using var refresh = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/{realm}/refresh");
        await HttpTestRequests.AddProofAsync(client, refresh, "");
        refresh.Headers.Add("Cookie", refreshCookie);
        using var rotated = await client.SendAsync(refresh);
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var replacement = rotated.Headers.GetValues("Set-Cookie")
            .Single(cookie => cookie.StartsWith("__Host-brouj-refresh=", StringComparison.Ordinal)).Split(';')[0];
        Assert.NotEqual(refreshCookie, replacement);

        using var replay = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/{realm}/refresh");
        await HttpTestRequests.AddProofAsync(client, replay, "");
        replay.Headers.Add("Cookie", refreshCookie);
        using var replayed = await client.SendAsync(replay);
        Assert.Equal(HttpStatusCode.Unauthorized, replayed.StatusCode);

        using var profile = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/{realm}/me");
        profile.Headers.Add("Cookie", accessCookie);
        using var authenticated = await client.SendAsync(profile);
        Assert.Equal(HttpStatusCode.OK, authenticated.StatusCode);

        using var logout = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/{realm}/logout");
        await HttpTestRequests.AddProofAsync(client, logout, "");
        logout.Headers.Add("Cookie", replacement);
        using var loggedOut = await client.SendAsync(logout);
        Assert.Equal(HttpStatusCode.OK, loggedOut.StatusCode);
        Assert.Equal(2, loggedOut.Headers.GetValues("Set-Cookie").Count());
        Assert.All(loggedOut.Headers.GetValues("Set-Cookie"), cookie => Assert.Contains("expires=", cookie));
        Assert.DoesNotContain(factory.Logs.Messages, message => message.Contains(password, StringComparison.Ordinal) ||
            message.Contains(refreshCookie, StringComparison.Ordinal) || message.Contains(hash, StringComparison.Ordinal));
    }
}
