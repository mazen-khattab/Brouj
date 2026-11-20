using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class InitiativeReservationMigrationTests
{
    [Fact]
    public void Baseline_creates_initiative_profiles_with_a_shared_key_and_cascading_delete()
    {
        using var context = TestContext.Create();
        var baseline = context.Database.GetMigrations().First();
        var script = context.GetService<IMigrator>().GenerateScript(toMigration: baseline);
        Assert.Contains("CREATE TABLE [InitiativeReservations]", script);
        Assert.Contains("CONSTRAINT [PK_InitiativeReservations] PRIMARY KEY ([ReservationId])", script);
        Assert.Contains(
            "CONSTRAINT [FK_InitiativeReservations_Reservations_ReservationId] FOREIGN KEY ([ReservationId]) REFERENCES [Reservations] ([Id]) ON DELETE CASCADE",
            script);
    }
}
