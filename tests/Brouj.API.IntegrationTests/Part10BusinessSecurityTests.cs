using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Brouj.API.IntegrationTests.Support;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;

namespace Brouj.API.IntegrationTests;

/// <summary>Exercises real HTTP handlers and actor decisions; separate SQL tests verify database behavior.</summary>
public sealed class Part10BusinessSecurityTests
{
    [Theory]
    [InlineData("CustomerId")]
    [InlineData("UserId")]
    [InlineData("Role")]
    [InlineData("Status")]
    [InlineData("TotalPrice")]
    [InlineData("Price")]
    [InlineData("Number")]
    [InlineData("$type")]
    public async Task Protected_fields_and_type_metadata_cannot_be_overposted(string field)
    {
        using var factory = new Part8ApiFactory();
        using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(new Dictionary<string, object>
            {
                ["UnitId"] = Guid.NewGuid(),
                [field] = "attacker-controlled-value"
            })
        };
        await HttpTestRequests.AddProofAsync(client, request, factory.Credential("CustomerOnly"));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(factory.Recording.LastRequest);
    }

    [Fact]
    public async Task Order_creation_ignores_body_price_status_and_reservation_overrides()
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var unit = new BaseUnit
        {
            Id = Guid.NewGuid(),
            Number = "Security unit",
            Size = 10,
            MeterPrice = 25,
            Project = new BaseProject { ProjectType = ProjectType.Regular }
        };
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            Unit = unit,
            UnitId = unit.Id,
            CustomerId = factory.CustomerId,
            Status = ReservationStatus.Processing,
            Number = "Security reservation"
        };
        factory.Database.BaseUnits.Add(unit);
        factory.Database.Reservations.Add(reservation);
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/admin/reservations/{reservation.Id}/order")
        {
            Content = JsonContent.Create(new
            {
                TotalPrice = 0,
                Status = "completed",
                ReservationId = Guid.NewGuid(),
                CustomerId = factory.OtherCustomerId
            })
        };
        await HttpTestRequests.AddProofAsync(client, request, factory.Credential("AdminOnly"));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = Assert.Single(factory.Database.Orders);
        Assert.Equal(250m, order.TotalPrice);
        Assert.Equal(OrderStatus.Processing, order.Status);
        Assert.Equal(reservation.Id, order.ReservationId);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task No_unapproved_reservation_update_route_allows_customer_mutations(string method)
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = factory.OtherCustomerId,
            Status = ReservationStatus.Processing
        };
        factory.Database.Reservations.Add(reservation);
        using var request = new HttpRequestMessage(new HttpMethod(method), $"/api/v1/reservations/{reservation.Id}")
        {
            Content = JsonContent.Create(new { CustomerId = factory.CustomerId, Status = "cancelled" })
        };
        await HttpTestRequests.AddProofAsync(client, request, factory.Credential("CustomerOnly"));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(factory.OtherCustomerId, reservation.CustomerId);
        Assert.Equal(ReservationStatus.Processing, reservation.Status);
    }

    [Fact]
    public async Task Customer_order_detail_checks_ownership_even_under_the_admin_route_prefix()
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = factory.OtherCustomerId,
            Unit = new BaseUnit { Project = new BaseProject { Name = "Private project" } }
        };
        var order = new Order { Id = Guid.NewGuid(), Reservation = reservation, Number = "PRIVATE-ORDER" };
        factory.Database.Orders.Add(order);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Credential("CustomerOnly"));

        using var response = await client.GetAsync(
            $"/api/v1/admin/orders/{order.Id}?customerId={factory.OtherCustomerId}&role=super%20admin");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("PRIVATE-ORDER", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stored_script_content_is_returned_as_JSON_data_with_private_cache_controls(bool requestHtml)
    {
        const string payload = "<script>alert('stored-xss')</script>";
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        factory.Database.Cities.Add(new City { Id = Guid.NewGuid(), Name = payload });

        if (requestHtml)
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        }

        using var response = await client.GetAsync("/api/v1/cities?search=" + Uri.EscapeDataString(payload));
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(payload, json.RootElement.GetProperty("data").GetProperty("items")[0].GetProperty("name").GetString());
        // JSON string data is allowed to contain markup. The security contract is a JSON response
        // with nosniff/CSP, even when HTML is requested; clients must render these fields as text.
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        Assert.Contains("default-src 'none'", Assert.Single(response.Headers.GetValues("Content-Security-Policy")));
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.DoesNotContain(factory.Logs.Messages, message => message.Contains(payload, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("sort=Name%3BDROP%20TABLE%20Users%3B--")]
    [InlineData("filter=CustomerId%20eq%20null%20or%20true")]
    [InlineData("returnUrl=https%3A%2F%2Fevil.example")]
    public async Task Unsupported_sort_filter_and_redirect_inputs_cannot_change_query_execution(string query)
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        factory.Database.Cities.Add(new City { Id = Guid.NewGuid(), Name = "Expected city" });

        using var response = await client.GetAsync("/api/v1/cities?" + query);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Location"));
        Assert.Contains("Expected city", await response.Content.ReadAsStringAsync());
    }
}
