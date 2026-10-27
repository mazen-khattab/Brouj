using Brouj.Domain.Entities;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Brouj.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

/// <summary>Verifies the deployment migration against the approved model on disposable SQL databases.</summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class Part12MigrationTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Migration_snapshot_matches_the_model_and_release_SQL_is_generated_for_review()
    {
        await using var context = TestContext.Create();
        var migrations = context.Database.GetMigrations().ToArray();
        var migrator = context.GetService<IMigrator>();

        Assert.Equal("20261026163251_initialMigration", Assert.Single(migrations));
        Assert.False(context.Database.HasPendingModelChanges());

        var sql = migrator.GenerateScript();
        var idempotentSql = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);

        foreach (var entity in context.Model.GetEntityTypes())
        {
            Assert.Contains($"CREATE TABLE [{entity.GetTableName()}]", sql);
        }

        Assert.Contains("__EFMigrationsHistory", idempotentSql);
        Assert.Contains("IF NOT EXISTS", idempotentSql);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);

        // Generate review artifacts without starting the API or reading its developer secret configuration.
        var directory = Path.Combine(AppContext.BaseDirectory, "TestResults", "Part12");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "initialMigration.sql"), sql);
        await File.WriteAllTextAsync(Path.Combine(directory, "initialMigration.idempotent.sql"), idempotentSql);
    }

    [SqlServerFact]
    public async Task Clean_migration_matches_approved_schema_and_reapplying_preserves_data()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var cancellationToken = timeout.Token;
        using var logging = LoggerFactory.Create(builder => builder.AddSimpleConsole());
        var logger = logging.CreateLogger<Part12MigrationTests>();
        await using var migratedDatabase = await fixture.CreateDatabaseAsync(applyMigrations: true);
        await using var modelDatabase = await fixture.CreateDatabaseAsync();
        await using var migrated = migratedDatabase.CreateContext();
        await using var model = modelDatabase.CreateContext();

        logger.LogInformation("Comparing clean migrated SQL schema with the separately verified approved model");
        var migratedSchema = await ReadSchemaAsync(migrated, cancellationToken);
        var expectedSchema = await ReadSchemaAsync(model, cancellationToken);

        // Compare physical columns, keys, ordered/filtered indexes, FKs, checks and defaults, not just EF metadata.
        Assert.Equal(expectedSchema, migratedSchema);
        Assert.Equal(26, migratedSchema.Count(row => row.StartsWith("TABLE|", StringComparison.Ordinal)));
        Assert.Empty(await migrated.Database.GetPendingMigrationsAsync(cancellationToken));
        Assert.Equal(migrated.Database.GetMigrations(),
            await migrated.Database.GetAppliedMigrationsAsync(cancellationToken));

        var city = new City { Id = Guid.NewGuid(), Name = "Migration preservation check" };
        migrated.Cities.Add(city);
        await migrated.SaveChangesAsync(cancellationToken);
        await migrated.Database.MigrateAsync(cancellationToken);

        Assert.True(await migrated.Cities.AsNoTracking().AnyAsync(item => item.Id == city.Id, cancellationToken));
        Assert.Single(await migrated.Database.GetAppliedMigrationsAsync(cancellationToken));
        logger.LogInformation("Clean migration, physical schema comparison and repeat-application checks passed");
    }

    private static async Task<string[]> ReadSchemaAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using var command = context.Database.GetDbConnection().CreateCommand();

        // Catalog descriptions can use a different collation from object names. Normalize comparison text only.
        command.CommandText = """
            SELECT CONCAT(N'TABLE|', SCHEMA_NAME(t.schema_id), N'.', t.name) COLLATE DATABASE_DEFAULT AS Definition
            FROM sys.tables t WHERE t.is_ms_shipped = 0 AND t.name <> '__EFMigrationsHistory'
            UNION ALL
            SELECT CONCAT(N'COLUMN|', t.name, N'|', c.name, N'|', ty.name, N'|', c.max_length, N'|',
                c.precision, N'|', c.scale, N'|', c.is_nullable, N'|', c.is_identity, N'|', c.is_computed,
                N'|', COALESCE(d.definition, N''), N'|', COALESCE(computed.definition, N'')) COLLATE DATABASE_DEFAULT
            FROM sys.tables t JOIN sys.columns c ON c.object_id = t.object_id
            JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            LEFT JOIN sys.default_constraints d ON d.object_id = c.default_object_id
            LEFT JOIN sys.computed_columns computed ON computed.object_id = c.object_id AND computed.column_id = c.column_id
            WHERE t.is_ms_shipped = 0 AND t.name <> '__EFMigrationsHistory'
            UNION ALL
            SELECT CONCAT(N'INDEX|', t.name, N'|', i.name, N'|', i.is_unique, N'|', i.is_primary_key,
                N'|', i.is_unique_constraint, N'|', i.type_desc, N'|', COALESCE(i.filter_definition, N''),
                N'|', col.name, N'|', ic.key_ordinal, N'|', ic.is_descending_key, N'|', ic.is_included_column)
                COLLATE DATABASE_DEFAULT
            FROM sys.tables t JOIN sys.indexes i ON i.object_id = t.object_id
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns col ON col.object_id = ic.object_id AND col.column_id = ic.column_id
            WHERE t.is_ms_shipped = 0 AND t.name <> '__EFMigrationsHistory'
            UNION ALL
            SELECT CONCAT(N'FK|', dependent.name, N'|', fk.name, N'|', principal.name, N'|', dc.name, N'|', pc.name,
                N'|', link.constraint_column_id, N'|', fk.delete_referential_action_desc,
                N'|', fk.update_referential_action_desc, N'|', fk.is_disabled, N'|', fk.is_not_trusted)
                COLLATE DATABASE_DEFAULT
            FROM sys.foreign_keys fk JOIN sys.tables dependent ON dependent.object_id = fk.parent_object_id
            JOIN sys.tables principal ON principal.object_id = fk.referenced_object_id
            JOIN sys.foreign_key_columns link ON link.constraint_object_id = fk.object_id
            JOIN sys.columns dc ON dc.object_id = link.parent_object_id AND dc.column_id = link.parent_column_id
            JOIN sys.columns pc ON pc.object_id = link.referenced_object_id AND pc.column_id = link.referenced_column_id
            UNION ALL
            SELECT CONCAT(N'CHECK|', t.name, N'|', c.name, N'|', c.definition, N'|', c.is_disabled, N'|', c.is_not_trusted)
                COLLATE DATABASE_DEFAULT
            FROM sys.check_constraints c JOIN sys.tables t ON t.object_id = c.parent_object_id
            ORDER BY Definition;
            """;
        var rows = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(reader.GetString(0));
        }

        return rows.ToArray();
    }
}
