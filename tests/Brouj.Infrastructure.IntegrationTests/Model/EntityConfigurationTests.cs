using Brouj.Application.Abstractions.Persistence;
using Brouj.Domain.Entities;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Brouj.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Brouj.Infrastructure.IntegrationTests.Model;

public sealed class EntityConfigurationTests
{
    public static IEnumerable<object[]> Columns()
    {
        foreach (var line in File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "ApprovedColumns.tsv")).Skip(1))
        {
            var cells = line.Split('\t');
            yield return [cells[0], cells[1], cells[2], cells[3], bool.Parse(cells[4])];
        }
    }

    [Theory]
    [MemberData(nameof(Columns))]
    public void Every_column_matches_the_owner_approved_type_and_nullability(
        string entityName, string columnName, string clrName, string sqlType, bool nullable)
    {
        using var context = TestContext.Create();
        var entity = Assert.Single(context.Model.GetEntityTypes(), entity => entity.ClrType.Name == entityName);
        var property = Assert.IsAssignableFrom<IProperty>(entity.FindProperty(columnName));
        Assert.Equal(sqlType, property.GetColumnType());
        Assert.Equal(nullable, property.IsNullable);
        Assert.Equal(clrName.TrimEnd('?'), (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).Name switch
        {
            "String" => "string",
            "Int32" => "int",
            "Boolean" => "bool",
            "Decimal" => "decimal",
            var name => name
        });
        Assert.Equal(columnName, property.GetColumnName());

        if (sqlType.StartsWith("varchar(", StringComparison.Ordinal) ||
            sqlType.StartsWith("nvarchar(", StringComparison.Ordinal))
        {
            Assert.Equal(sqlType.StartsWith("nvarchar", StringComparison.Ordinal), property.IsUnicode() ?? true);
            var capacity = sqlType[(sqlType.IndexOf('(') + 1)..^1];
            if (capacity != "max")
            {
                Assert.Equal(int.Parse(capacity), property.GetMaxLength());
            }
        }

        if (sqlType.StartsWith("decimal(", StringComparison.Ordinal))
        {
            var parts = sqlType[8..^1].Split(',');
            Assert.Equal(int.Parse(parts[0]), property.GetPrecision());
            Assert.Equal(int.Parse(parts[1]), property.GetScale());
        }

        if (sqlType == "datetimeoffset(3)")
        {
            Assert.Equal(3, property.GetPrecision());
        }
    }

    [Fact]
    public void All_26_entities_have_a_configuration_table_primary_key_and_application_DbSet()
    {
        using var context = TestContext.Create();
        var modelEntities = context.Model.GetEntityTypes().ToArray();
        Assert.Equal(26, modelEntities.Length);
        Assert.Equal(181, modelEntities.Sum(entity => entity.GetProperties().Count()));
        Assert.Equal(181, Columns().Count());

        var configurations = typeof(ApplicationDbContext).Assembly.GetTypes()
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType &&
                    contract.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)))
            .Select(contract => contract.GetGenericArguments()[0])
            .ToArray();

        Assert.Equal(26, configurations.Length);
        Assert.IsAssignableFrom<IApplicationDbContext>(context);
        var dbSets = typeof(IApplicationDbContext).GetProperties();
        Assert.Equal(26, dbSets.Length);

        foreach (var entity in modelEntities)
        {
            Assert.Contains(entity.ClrType, configurations);
            var dbSet = Assert.Single(dbSets, property => property.PropertyType.GetGenericArguments()[0] == entity.ClrType);
            Assert.NotNull(dbSet.GetValue(context));
            Assert.Equal(dbSet.Name, entity.GetTableName());
            var key = Assert.Single(Assert.IsAssignableFrom<IKey>(entity.FindPrimaryKey()).Properties);
            Assert.Equal(entity.ClrType == typeof(InitiativeProject) ? "ProjectId" :
                entity.ClrType == typeof(InitiativeUnit) ? "UnitId" : "Id", key.Name);
            Assert.Equal(typeof(Guid), key.ClrType);
            Assert.DoesNotContain(entity.GetProperties(), property => property.IsShadowProperty());
        }
    }

    [Fact]
    public void No_unapproved_defaults_checks_alternate_keys_or_columns_are_added()
    {
        using var context = TestContext.Create();
        var model = context.GetService<IDesignTimeModel>().Model;

        foreach (var entity in model.GetEntityTypes())
        {
            Assert.Empty(entity.GetCheckConstraints());
            Assert.Single(entity.GetKeys());
            foreach (var property in entity.GetProperties())
            {
                Assert.Null(property.FindAnnotation(RelationalAnnotationNames.DefaultValue));
                Assert.Null(property.GetDefaultValueSql());
                Assert.Null(property.GetComputedColumnSql());
                Assert.Null(property.GetCollation());
            }
        }

        var project = model.FindEntityType(typeof(BaseProject))!;
        Assert.Null(project.FindProperty("Location"));
        Assert.True(project.FindProperty("Latitude")!.IsNullable);
        Assert.True(project.FindProperty("Longitude")!.IsNullable);
    }

    [Fact]
    public void Audit_payload_columns_are_optional_JSON_text()
    {
        using var context = TestContext.Create();
        var entity = context.Model.FindEntityType(typeof(ActivityLog))!;
        foreach (var name in new[] { "OldData", "NewData" })
        {
            var property = entity.FindProperty(name)!;
            Assert.Equal(typeof(string), property.ClrType);
            Assert.True(property.IsNullable);
            Assert.Equal("nvarchar(max)", property.GetColumnType());
        }
    }
}
