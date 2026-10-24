using Brouj.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class DeleteBehaviorTests(SqlServerFixture fixture)
{
    [SqlServerTheory]
    [InlineData("City")]
    [InlineData("Area")]
    [InlineData("Customer")]
    [InlineData("BaseUnit")]
    [InlineData("Reservation")]
    [InlineData("User")]
    [InlineData("BaseProject")]
    public async Task Protected_deletes_are_rejected_by_SQL_Server(string principal)
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;
        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, log: true);
        }

        await using (var context = database.CreateContext())
        {
            var (type, id) = principal switch
            {
                "City" => (typeof(City), graph.City.Id),
                "Area" => (typeof(Area), graph.Area.Id),
                "Customer" => (typeof(Customer), graph.Customer.Id),
                "BaseUnit" => (typeof(BaseUnit), graph.Unit.Id),
                "Reservation" => (typeof(Reservation), graph.Reservation.Id),
                "User" => (typeof(User), graph.User.Id),
                "BaseProject" => (typeof(BaseProject), graph.Project.Id),
                _ => throw new ArgumentException("Unknown principal.", nameof(principal))
            };
            // Only the principal is tracked: restrictions must be enforced by the database.
            context.Remove((await context.FindAsync(type, id))!);
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            Assert.Equal(547, Assert.IsType<SqlException>(error.InnerException).Number);
        }

        await using var reader = database.CreateContext();
        Assert.Equal(2, await reader.Reservations.AsNoTracking().CountAsync());
        Assert.Equal(2, await reader.BaseUnits.AsNoTracking().CountAsync());
    }

    [SqlServerTheory]
    [InlineData("BaseProject")]
    [InlineData("BaseUnit")]
    [InlineData("TimePlan")]
    [InlineData("StageTemplate")]
    [InlineData("ItemTemplate")]
    [InlineData("Customer")]
    [InlineData("User")]
    public async Task Approved_cascades_delete_only_their_dependents(string principal)
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;
        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, reservations: false);
        }

        await using (var writer = database.CreateContext())
        {
            var (type, id) = principal switch
            {
                "BaseProject" => (typeof(BaseProject), graph.Project.Id),
                "BaseUnit" => (typeof(BaseUnit), graph.Unit.Id),
                "TimePlan" => (typeof(TimePlan), graph.Plan.Id),
                "StageTemplate" => (typeof(StageTemplate), graph.StageTemplate.Id),
                "ItemTemplate" => (typeof(ItemTemplate), graph.ItemTemplate.Id),
                "Customer" => (typeof(Customer), graph.Customer.Id),
                "User" => (typeof(User), graph.User.Id),
                _ => throw new ArgumentException("Unknown principal.", nameof(principal))
            };
            writer.Remove((await writer.FindAsync(type, id))!);
            await writer.SaveChangesAsync();
        }

        await using var reader = database.CreateContext();

        if (principal is "BaseProject" or "BaseUnit")
        {
            Assert.Equal(principal == "BaseProject" ? 0 : 1, await reader.BaseUnits.AsNoTracking().CountAsync());
            Assert.Equal(0, await reader.InitiativeUnits.AsNoTracking().CountAsync());
            Assert.Equal(0, await reader.UnitAmenities.AsNoTracking().CountAsync());
            Assert.Equal(0, await reader.UnitImages.AsNoTracking().CountAsync());
        }

        if (principal is "BaseProject")
        {
            Assert.Equal(0, await reader.InitiativeProjects.AsNoTracking().CountAsync());
            Assert.Equal(0, await reader.ProjectAmenities.AsNoTracking().CountAsync());
            Assert.Equal(0, await reader.ProjectImages.AsNoTracking().CountAsync());
        }

        if (principal is "BaseProject" or "TimePlan")
        {
            Assert.Equal(0, await reader.TimePlans.AsNoTracking().CountAsync());
            Assert.Equal(0, await reader.TimePlanStages.AsNoTracking().CountAsync());
            Assert.Equal(0, await reader.PlanStageItems.AsNoTracking().CountAsync());
            Assert.Equal(0, await reader.StageItemImages.AsNoTracking().CountAsync());
        }

        if (principal is "StageTemplate" or "ItemTemplate")
        {
            Assert.Equal(0, await reader.StageTemplateItems.AsNoTracking().CountAsync());
            Assert.Equal(principal == "StageTemplate" ? 1 : 0, await reader.ItemTemplates.AsNoTracking().CountAsync());
            Assert.Equal(principal == "ItemTemplate" ? 1 : 0, await reader.StageTemplates.AsNoTracking().CountAsync());
        }

        if (principal == "Customer")
        {
            Assert.Equal(0, await reader.CustomerRefreshTokens.AsNoTracking().CountAsync());
        }

        if (principal == "User")
        {
            Assert.Equal(0, await reader.UserRefreshTokens.AsNoTracking().CountAsync());
        }

        Assert.Equal(0, await reader.Reservations.AsNoTracking().CountAsync());
    }
}
