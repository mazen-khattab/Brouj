using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.IntegrationTests.Model;
using Brouj.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class EnumPersistenceTests(SqlServerFixture fixture)
{
    [SqlServerTheory]
    [MemberData(nameof(EnumConverterTests.Members), MemberType = typeof(EnumConverterTests))]
    public async Task Every_member_is_stored_as_its_exact_approved_string_and_materializes(
        Type enumType, string member, string persisted)
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();
        var graph = await TestGraph.SeedAsync(context, log: true);
        var target = Target(enumType, graph);
        var value = Enum.Parse(enumType, member);
        target.Entity.GetType().GetProperty(target.Property)!.SetValue(target.Entity, value);
        await context.SaveChangesAsync();

        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = target.ReadSql;
        command.Parameters.Add(new SqlParameter("@id", target.Id));
        Assert.Equal(persisted, await command.ExecuteScalarAsync());
        context.ChangeTracker.Clear();
        var loaded = (await context.FindAsync(target.Entity.GetType(), target.Id))!;
        Assert.Equal(value, loaded.GetType().GetProperty(target.Property)!.GetValue(loaded));
    }

    [SqlServerTheory]
    [MemberData(nameof(EnumConverterTests.Types), MemberType = typeof(EnumConverterTests))]
    public async Task Unknown_persisted_enum_strings_fail_during_materialization(Type enumType)
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();
        var graph = await TestGraph.SeedAsync(context, log: true);
        var target = Target(enumType, graph);
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = target.UpdateSql;
        command.Parameters.Add(new SqlParameter("@value", "unknown"));
        command.Parameters.Add(new SqlParameter("@id", target.Id));
        await command.ExecuteNonQueryAsync();
        context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await context.FindAsync(target.Entity.GetType(), target.Id);
        });
    }

    private static EnumTarget Target(Type type, TestGraph graph) =>
        type == typeof(ProjectType) ? new(graph.Project, graph.Project.Id, "ProjectType",
            "SELECT [ProjectType] FROM [BaseProjects] WHERE [Id] = @id",
            "UPDATE [BaseProjects] SET [ProjectType] = @value WHERE [Id] = @id") :
        type == typeof(ReservationStatus) ? new(graph.Reservation, graph.Reservation.Id, "Status",
            "SELECT [Status] FROM [Reservations] WHERE [Id] = @id",
            "UPDATE [Reservations] SET [Status] = @value WHERE [Id] = @id") :
        type == typeof(OrderStatus) ? new(graph.Order, graph.Order.Id, "Status",
            "SELECT [Status] FROM [Orders] WHERE [Id] = @id",
            "UPDATE [Orders] SET [Status] = @value WHERE [Id] = @id") :
        type == typeof(PlanStageItemStatus) ? new(graph.Item, graph.Item.Id, "Status",
            "SELECT [Status] FROM [PlanStageItems] WHERE [Id] = @id",
            "UPDATE [PlanStageItems] SET [Status] = @value WHERE [Id] = @id") :
        type == typeof(UserRole) ? new(graph.User, graph.User.Id, "Role",
            "SELECT [Role] FROM [Users] WHERE [Id] = @id",
            "UPDATE [Users] SET [Role] = @value WHERE [Id] = @id") :
        type == typeof(ActivityAction) ? new(graph.Log, graph.Log.Id, "Action",
            "SELECT [Action] FROM [ActivityLogs] WHERE [Id] = @id",
            "UPDATE [ActivityLogs] SET [Action] = @value WHERE [Id] = @id") :
        throw new ArgumentException("Unknown test enum type.", nameof(type));

    private sealed record EnumTarget(object Entity, Guid Id, string Property, string ReadSql, string UpdateSql);
}
