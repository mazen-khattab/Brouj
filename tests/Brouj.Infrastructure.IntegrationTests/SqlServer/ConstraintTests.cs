using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class ConstraintTests(SqlServerFixture fixture)
{
    [SqlServerTheory]
    [InlineData("Reservation.Number", "UQ_Reservations_Number")]
    [InlineData("Order.Number", "UQ_Orders_Number")]
    [InlineData("Order.ReservationId", "UQ_Orders_ReservationId")]
    [InlineData("TimePlan.ProjectId", "UQ_TimePlans_ProjectId")]
    [InlineData("ProcessingReservation", "UQ_Reservations_CustomerId_UnitId_Processing")]
    public async Task Approved_unique_rules_reject_duplicates(string rule, string constraint)
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();
        var graph = await TestGraph.SeedAsync(context);
        context.ChangeTracker.Clear();

        object duplicate = rule switch
        {
            "Reservation.Number" => new Reservation
            {
                CustomerId = graph.Customer.Id, UnitId = graph.OtherUnit.Id,
                Number = graph.Reservation.Number, Status = ReservationStatus.Confirmed, ReservationDate = DateTimeOffset.UtcNow
            },
            "Order.Number" => new Order { ReservationId = graph.OtherReservation.Id, Number = graph.Order.Number, TotalPrice = 500 },
            "Order.ReservationId" => new Order { ReservationId = graph.Reservation.Id, Number = "O2", TotalPrice = 500 },
            "TimePlan.ProjectId" => new TimePlan { ProjectId = graph.Project.Id, Name = "second", DeliveryDate = DateTimeOffset.UtcNow },
            "ProcessingReservation" => graph.NewReservation("R3"),
            _ => throw new ArgumentException("Unknown test rule.", nameof(rule))
        };
        context.Add(duplicate);

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        var sql = Assert.IsType<SqlException>(error.InnerException);
        Assert.Contains(sql.Number, new[] { 2601, 2627 });
        Assert.Contains(constraint, sql.Message, StringComparison.Ordinal);
    }

    [SqlServerFact]
    public async Task Cancellation_allows_a_new_processing_reservation_for_the_same_pair()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();
        var graph = await TestGraph.SeedAsync(context, order: false);
        graph.Reservation.Status = ReservationStatus.Cancelled;
        graph.Reservation.CancelledAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
        context.Reservations.Add(graph.NewReservation("R3"));

        await context.SaveChangesAsync();

        Assert.Equal(1, await context.Reservations.AsNoTracking().CountAsync(reservation =>
            reservation.CustomerId == graph.Customer.Id && reservation.UnitId == graph.Unit.Id &&
            reservation.Status == ReservationStatus.Processing));
    }

    [SqlServerFact]
    public async Task Two_concurrent_processing_inserts_commit_at_most_one_row()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;
        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, reservations: false);
        }

        async Task<bool> InsertAsync(string number)
        {
            await using var writer = database.CreateContext();
            writer.Reservations.Add(graph.NewReservation(number));
            try
            {
                await writer.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
            {
                return false;
            }
        }

        var committed = await Task.WhenAll(InsertAsync("concurrent-1"), InsertAsync("concurrent-2"));
        Assert.Single(committed, value => value);
        await using var reader = database.CreateContext();
        Assert.Equal(1, await reader.Reservations.AsNoTracking().CountAsync(reservation =>
            reservation.CustomerId == graph.Customer.Id && reservation.UnitId == graph.Unit.Id &&
            reservation.Status == ReservationStatus.Processing));
    }

    [SqlServerFact]
    public async Task Soft_deleted_customer_contact_values_can_be_reused()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();
        var graph = await TestGraph.SeedAsync(context, reservations: false);
        graph.Customer.DeletedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
        context.Customers.Add(new Customer
        {
            FName = "replacement", LName = "customer", Email = graph.Customer.Email,
            Phone = graph.Customer.Phone, IdentityNumber = graph.Customer.IdentityNumber, HashPassword = "synthetic-hash"
        });
        await context.SaveChangesAsync();
        Assert.Equal(2, await context.Customers.AsNoTracking().CountAsync());
    }

    [SqlServerFact]
    public async Task Only_active_terms_versions_are_unique()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();
        await TestGraph.SeedAsync(context, reservations: false);
        var inactive = new TermsAndConditions { Title = "inactive", Content = "terms", VersionNumber = 1, EffectiveDate = DateTimeOffset.UtcNow, IsActive = false };
        context.TermsAndConditions.Add(inactive);
        await context.SaveChangesAsync();
        inactive.IsActive = true;
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.IsType<SqlException>(error.InnerException);
    }
}
