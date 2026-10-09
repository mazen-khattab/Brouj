using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Infrastructure.IntegrationTests.Model;

public sealed class IndexTests
{
    [Fact]
    public void Exact_index_set_matches_approved_additions_and_FK_conventions()
    {
        using var context = TestContext.Create();
        var expected = File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "ApprovedIndexes.tsv"))
            .Skip(1).Select(line => line.Split('\t')).ToArray();
        var actual = context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetIndexes().Select(index => new
            {
                Entity = entity.ClrType.Name,
                Name = index.GetDatabaseName(),
                Columns = string.Join(",", index.Properties.Select(property => property.Name)),
                index.IsUnique,
                Filter = index.GetFilter() ?? ""
            })).ToArray();

        Assert.Equal(expected.Length, actual.Length);
        foreach (var row in expected)
        {
            var index = Assert.Single(actual, index => index.Entity == row[0] && index.Name == row[1]);
            Assert.Equal(row[2], index.Columns);
            Assert.Equal(bool.Parse(row[3]), index.IsUnique);
            Assert.Equal(row.ElementAtOrDefault(4) ?? "", index.Filter);
        }

        Assert.DoesNotContain(actual, index => index.Name is
            "IX_CustomerRefreshTokens_CustomerId" or "IX_UserRefreshTokens_UserId" or
            "IX_Orders_ReservationId" or "IX_TimePlans_ProjectId");
    }
}
