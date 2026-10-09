using Brouj.Infrastructure.IntegrationTests.Model;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class SchemaPersistenceTests(SqlServerFixture fixture)
{
    [SqlServerFact]
    public async Task All_181_columns_have_the_approved_SQL_Server_types_and_nullability()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT t.name, c.name, ty.name, c.max_length, c.precision, c.scale, c.is_nullable
            FROM sys.tables AS t
            INNER JOIN sys.columns AS c ON c.object_id = t.object_id
            INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
            ORDER BY t.name, c.column_id
            """;
        var columns = new Dictionary<(string Table, string Column), (string Type, bool Nullable)>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var type = reader.GetString(2);
                var length = reader.GetInt16(3);
                var precision = reader.GetByte(4);
                var scale = reader.GetByte(5);
                type = type switch
                {
                    "nvarchar" => $"nvarchar({(length == -1 ? "max" : (length / 2).ToString())})",
                    "varchar" => $"varchar({(length == -1 ? "max" : length.ToString())})",
                    "decimal" => $"decimal({precision},{scale})",
                    "datetimeoffset" => $"datetimeoffset({scale})",
                    _ => type
                };
                columns.Add((reader.GetString(0), reader.GetString(1)), (type, reader.GetBoolean(6)));
            }
        }

        Assert.Equal(181, columns.Count);
        foreach (var expected in EntityConfigurationTests.Columns())
        {
            var entity = Assert.Single(context.Model.GetEntityTypes(), entity => entity.ClrType.Name == (string)expected[0]);
            var persisted = columns[(entity.GetTableName()!, (string)expected[1])];
            Assert.Equal((string)expected[3], persisted.Type);
            Assert.Equal((bool)expected[4], persisted.Nullable);
        }
    }

    [SqlServerFact]
    public async Task All_22_FKs_use_the_approved_database_delete_action()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT dependent.name, principal.name, col.name, fk.delete_referential_action_desc
            FROM sys.foreign_keys AS fk
            INNER JOIN sys.tables AS dependent ON dependent.object_id = fk.parent_object_id
            INNER JOIN sys.tables AS principal ON principal.object_id = fk.referenced_object_id
            INNER JOIN sys.foreign_key_columns AS link ON link.constraint_object_id = fk.object_id
            INNER JOIN sys.columns AS col ON col.object_id = link.parent_object_id AND col.column_id = link.parent_column_id
            """;
        var fks = new Dictionary<(string Dependent, string Principal, string Column), string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                fks.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)), reader.GetString(3));
            }
        }

        Assert.Equal(22, fks.Count);
        foreach (var expected in RelationshipTests.Relationships())
        {
            var dependent = context.Model.FindEntityType((Type)expected[0])!.GetTableName()!;
            var principal = context.Model.FindEntityType((Type)expected[1])!.GetTableName()!;
            Assert.Equal((DeleteBehavior)expected[3] == DeleteBehavior.Cascade ? "CASCADE" : "NO_ACTION",
                fks[(dependent, principal, (string)expected[2])]);
        }
    }
}
