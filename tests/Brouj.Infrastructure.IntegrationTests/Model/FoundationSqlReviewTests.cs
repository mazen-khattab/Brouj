using System.Data;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Brouj.Infrastructure.IntegrationTests.Model;

/// <summary>Inspects SQL Server translation and DDL without claiming database execution or query plans.</summary>
public sealed class FoundationSqlReviewTests(ITestOutputHelper output)
{
    [Fact]
    public void Generated_DDL_contains_every_approved_index_and_no_exact_duplicates()
    {
        using var context = TestContext.Create();
        var script = context.Database.GenerateCreateScript();
        var expected = File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "ApprovedIndexes.tsv"))
            .Skip(1)
            .Select(line => line.Split('\t'))
            .ToArray();
        var indexes = context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetIndexes().Select(index => new
            {
                Table = entity.GetTableName(),
                Columns = string.Join(",", index.Properties.Select(property => property.GetColumnName())),
                index.IsUnique,
                Filter = index.GetFilter() ?? string.Empty
            }))
            .ToArray();

        // Identical columns, uniqueness and filter on one table would duplicate write maintenance.
        // Prefix overlap alone does not prove redundancy, especially for filtered unique indexes.
        Assert.All(indexes.GroupBy(index => index), group => Assert.Single(group));
        Assert.Equal(expected.Length, indexes.Length);

        foreach (var row in expected)
        {
            var entity = Assert.Single(context.Model.GetEntityTypes(), entity => entity.ClrType.Name == row[0]);
            var unique = bool.Parse(row[3]) ? "UNIQUE " : string.Empty;
            var columns = string.Join(", ", row[2].Split(',').Select(column => $"[{column}]"));
            var filter = string.IsNullOrEmpty(row.ElementAtOrDefault(4)) ? string.Empty : " WHERE " + row[4];
            var statement = $"CREATE {unique}INDEX [{row[1]}] ON [{entity.GetTableName()}] ({columns}){filter};";

            Assert.Contains(statement, script, StringComparison.Ordinal);
            output.WriteLine(statement);
        }

        output.WriteLine("Reviewed {0} secondary indexes; no database connection was opened.", indexes.Length);
    }

    [Fact]
    public void Existing_audit_timestamp_test_lookup_is_parameterized_and_bounded()
    {
        using var context = TestContext.Create();
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // AuditPersistenceTests reads a city with SingleAsync(id). Take(2) represents the same
        // bounded cardinality check so its representative SELECT can be inspected offline.
        var query = context.Cities.AsNoTracking().Where(city => city.Id == id).Take(2);
        using var command = query.CreateDbCommand();

        Assert.Contains("TOP(", command.CommandText);
        Assert.Contains(".[Id] = @", command.CommandText);
        Assert.DoesNotContain("JOIN", command.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(command.Parameters.Cast<SqlParameter>(), parameter =>
            parameter.SqlDbType == SqlDbType.UniqueIdentifier && Equals(parameter.Value, id));
        output.WriteLine(query.ToQueryString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Existing_refresh_lookup_projects_only_session_columns_with_an_ANSI_digest_parameter(bool user)
    {
        using var context = TestContext.Create();
        var digest = new string('A', 64);
        var query = user
            ? context.UserRefreshTokens.AsNoTracking()
                .Where(token => token.Token == digest)
                .Select(token => new SessionRow(token.Id, token.UserId, token.IsActive, token.ExpDate))
                .Take(2)
            : context.CustomerRefreshTokens.AsNoTracking()
                .Where(token => token.Token == digest)
                .Select(token => new SessionRow(token.Id, token.CustomerId, token.IsActive, token.ExpDate))
                .Take(2);
        using var command = query.CreateDbCommand();

        Assert.Contains("TOP(", command.CommandText);
        Assert.Contains(".[Token] = @", command.CommandText);
        Assert.DoesNotContain("JOIN", command.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".[CreatedAt]", command.CommandText);
        Assert.DoesNotContain("SELECT *", command.CommandText);
        var parameter = Assert.Single(command.Parameters.Cast<SqlParameter>(), parameter => Equals(parameter.Value, digest));
        Assert.Equal(SqlDbType.VarChar, parameter.SqlDbType);
        output.WriteLine(query.ToQueryString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Existing_actor_lock_queries_keep_fixed_lock_hints_and_narrow_outer_projections(bool user)
    {
        using var context = TestContext.Create();
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var query = user
            ? context.Users.FromSqlInterpolated($"SELECT * FROM [Users] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
                .AsNoTracking()
                .Select(actor => new ActorRow(actor.DeletedAt != null, actor.Role))
                .Take(2)
            : context.Customers.FromSqlInterpolated($"SELECT * FROM [Customers] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
                .AsNoTracking()
                .Select(actor => new ActorRow(actor.DeletedAt != null, UserRole.Admin))
                .Take(2);
        using var command = query.CreateDbCommand();

        Assert.Contains("WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = @", command.CommandText);
        Assert.DoesNotContain("HashPassword", command.CommandText);
        Assert.DoesNotContain("IdentityNumber", command.CommandText);
        Assert.Contains(command.Parameters.Cast<SqlParameter>(), parameter =>
            parameter.SqlDbType == SqlDbType.UniqueIdentifier && Equals(parameter.Value, id));
        output.WriteLine(query.ToQueryString());
    }

    private sealed record SessionRow(
        Guid Id,
        Guid ActorId,
        bool Active,
        DateTimeOffset Expires);

    private sealed record ActorRow(
        bool Deleted,
        UserRole Role);
}
