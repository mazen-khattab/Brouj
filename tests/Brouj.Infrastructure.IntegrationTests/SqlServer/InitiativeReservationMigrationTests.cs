using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class InitiativeReservationMigrationTests(SqlServerFixture fixture)
{
    private const string Previous = "20261118143206_AlignExistingUnitAndTimePlanSchema";
    private const string Current = "20261118143207_ExtractInitiativeReservationProfiles";

    [Fact]
    public void Snapshot_and_profile_migration_match_the_current_model()
    {
        using var context = TestContext.Create();
        Assert.False(context.Database.HasPendingModelChanges());
        var script = context.GetService<IMigrator>().GenerateScript(Previous, Current);
        Assert.True(script.IndexOf("INSERT INTO [InitiativeReservations]", StringComparison.Ordinal) <
            script.IndexOf("DROP COLUMN [IdentityNumber]", StringComparison.Ordinal));
        Assert.Contains("INNER JOIN [InitiativeUnits]", script);
        Assert.Contains("ON DELETE CASCADE", script);
        Assert.DoesNotContain("ALTER TABLE [TimePlanStages]", script);
        Assert.DoesNotContain("ALTER TABLE [BaseUnits]", script);
    }

    [SqlServerFact]
    public async Task Upgrade_copies_only_initiative_profiles_and_downgrade_restores_them()
    {
        await using var database = await fixture.CreateDatabaseAsync(applyMigrations: true);
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();
        var graph = await TestGraph.SeedAsync(context, order: false);
        await migrator.MigrateAsync(Previous);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [Reservations] SET [IdentityNumber] = '29001011234567', [AgeGroup] = '25-35',
                [PlaceOfResidence] = N'Cairo', [FinancialBracket] = 2, [RepaymentPeriod] = 24
            WHERE [Id] = {graph.Reservation.Id};
            """);

        await migrator.MigrateAsync(Current);
        context.ChangeTracker.Clear();
        var profile = Assert.Single(await context.InitiativeReservations.ToListAsync());
        Assert.Equal(graph.Reservation.Id, profile.ReservationId);
        Assert.Equal("29001011234567", profile.IdentityNumber);
        Assert.Equal("25-35", profile.AgeGroup);
        Assert.Equal("Cairo", profile.PlaceOfResidence);
        Assert.Equal(2, profile.FinancialBracket);
        Assert.Equal(24, profile.RepaymentPeriod);
        Assert.Equal(graph.Reservation.CreatedAt, profile.CreatedAt);

        await migrator.MigrateAsync(Previous);
        var restored = await context.Database.SqlQuery<string>($"""
            SELECT [IdentityNumber] AS [Value] FROM [Reservations] WHERE [Id] = {graph.Reservation.Id}
            """).SingleAsync();
        Assert.Equal("29001011234567", restored);
    }
}
