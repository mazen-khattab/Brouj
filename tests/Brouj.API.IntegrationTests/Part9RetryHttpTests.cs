using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Brouj.API.IntegrationTests.Support;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Errors;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;

namespace Brouj.API.IntegrationTests;

/// <summary>Checks retry/conflict HTTP contracts; SQL locking and uniqueness run separately in Testcontainers.</summary>
public sealed class Part9RetryHttpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Repeating_a_successful_create_returns_409_without_another_save(bool createOrder)
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var initialSaveCount = factory.Database.SaveCount;
        var unit = SeedUnit(factory);
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            UnitId = unit.Id,
            Unit = unit,
            CustomerId = factory.CustomerId,
            Status = ReservationStatus.Processing,
            Number = "retry-source"
        };

        if (createOrder)
        {
            factory.Database.Reservations.Add(reservation);
        }

        var path = createOrder
            ? $"/api/v1/admin/reservations/{reservation.Id}/order"
            : "/api/v1/reservations";
        var credential = factory.Credential(createOrder ? "AdminOnly" : "CustomerOnly");

        async Task<HttpResponseMessage> CreateAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path);

            if (!createOrder)
            {
                request.Content = JsonContent.Create(new { UnitId = unit.Id });
            }

            await HttpTestRequests.AddProofAsync(client, request, credential);

            return await client.SendAsync(request);
        }

        using var created = await CreateAsync();
        using var repeated = await CreateAsync();

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        Assert.Equal(initialSaveCount + 1, factory.Database.SaveCount);
        Assert.Single(factory.Database.Reservations);
        Assert.Equal(createOrder ? 1 : 0, factory.Database.Orders.Count());
    }

    [Theory]
    [InlineData(false, PersistenceConflict.ActiveReservation)]
    [InlineData(false, PersistenceConflict.ConcurrentWrite)]
    [InlineData(true, PersistenceConflict.ReservationOrder)]
    [InlineData(true, PersistenceConflict.ConcurrentWrite)]
    public async Task Database_conflicts_return_a_safe_feature_conflict(bool createOrder, PersistenceConflict conflict)
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var unit = SeedUnit(factory);
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            UnitId = unit.Id,
            Unit = unit,
            CustomerId = factory.CustomerId,
            Status = ReservationStatus.Processing,
            Number = "conflict-source"
        };

        if (createOrder)
        {
            factory.Database.Reservations.Add(reservation);
        }

        // Infrastructure emits only this safe category after rollback. HTTP uses the real mapping behavior.
        factory.Database.SaveException = new PersistenceConflictException(conflict);
        var path = createOrder
            ? $"/api/v1/admin/reservations/{reservation.Id}/order"
            : "/api/v1/reservations";
        using var request = new HttpRequestMessage(HttpMethod.Post, path);

        if (!createOrder)
        {
            request.Content = JsonContent.Create(new { UnitId = unit.Id });
        }

        await HttpTestRequests.AddProofAsync(client, request,
            factory.Credential(createOrder ? "AdminOnly" : "CustomerOnly"));

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var expectedCode = createOrder ? FeatureErrorCode.OrderConflict
            : conflict == PersistenceConflict.ActiveReservation ? FeatureErrorCode.ReservationAlreadyActive
            : FeatureErrorCode.ReservationConflict;

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal((int)expectedCode, json.RootElement.GetProperty("code").GetInt32());
        Assert.DoesNotContain("PersistenceConflictException", body);
    }

    private static BaseUnit SeedUnit(Part8ApiFactory factory)
    {
        var unit = new BaseUnit
        {
            Id = Guid.NewGuid(),
            Number = "retry-unit",
            Size = 10,
            MeterPrice = 5,
            Project = new BaseProject { Id = Guid.NewGuid(), Name = "Project", ProjectType = ProjectType.Regular }
        };

        factory.Database.BaseUnits.Add(unit);

        return unit;
    }
}
