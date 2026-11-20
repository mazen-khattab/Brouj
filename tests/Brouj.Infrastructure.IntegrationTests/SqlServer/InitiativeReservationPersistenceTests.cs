using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class InitiativeReservationPersistenceTests(SqlServerFixture fixture)
{
    [SqlServerFact]
    public async Task Profile_graph_is_saved_and_deleted_with_its_reservation()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();
        var graph = await TestGraph.SeedAsync(context, order: false);
        graph.Reservation.InitiativeReservation = Profile(graph.Reservation.Id);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var reservation = await context.Reservations.Include(row => row.InitiativeReservation)
            .SingleAsync(row => row.Id == graph.Reservation.Id);
        Assert.Equal("29001011234567", reservation.InitiativeReservation!.IdentityNumber);
        Assert.Null((await context.Reservations.Include(row => row.InitiativeReservation)
            .SingleAsync(row => row.Id == graph.OtherReservation.Id)).InitiativeReservation);

        // Clear tracking to verify the database cascade, rather than EF deleting a tracked dependent.
        context.ChangeTracker.Clear();
        await context.Reservations.Where(row => row.Id == graph.Reservation.Id).ExecuteDeleteAsync();
        Assert.False(await context.InitiativeReservations.AnyAsync());
    }

    [SqlServerFact]
    public async Task Profile_cannot_reference_a_missing_reservation()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();
        context.InitiativeReservations.Add(Profile(Guid.NewGuid()));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task Profile_columns_are_required_in_the_database()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();
        var graph = await TestGraph.SeedAsync(context, order: false);
        var profile = Profile(graph.Reservation.Id);
        profile.IdentityNumber = null!;
        context.InitiativeReservations.Add(profile);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static InitiativeReservation Profile(Guid reservationId) => new()
    {
        ReservationId = reservationId, IdentityNumber = "29001011234567", AgeGroup = "25-35",
        PlaceOfResidence = "Cairo", FinancialBracket = 1, RepaymentPeriod = 12
    };
}
