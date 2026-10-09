using Brouj.Domain.Entities;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Brouj.Infrastructure.IntegrationTests.Model;

public sealed class RelationshipTests
{
    public static IEnumerable<object[]> Relationships()
    {
        yield return [typeof(CustomerRefreshToken), typeof(Customer), "CustomerId", DeleteBehavior.Cascade, false];
        yield return [typeof(Reservation), typeof(Customer), "CustomerId", DeleteBehavior.Restrict, false];
        yield return [typeof(UserRefreshToken), typeof(User), "UserId", DeleteBehavior.Cascade, false];
        yield return [typeof(ActivityLog), typeof(User), "UserId", DeleteBehavior.Restrict, false];
        yield return [typeof(Area), typeof(City), "CityId", DeleteBehavior.Restrict, false];
        yield return [typeof(AreaAmenity), typeof(Area), "AreaId", DeleteBehavior.Cascade, false];
        yield return [typeof(BaseProject), typeof(Area), "AreaId", DeleteBehavior.Restrict, false];
        yield return [typeof(InitiativeProject), typeof(BaseProject), "ProjectId", DeleteBehavior.Cascade, true];
        yield return [typeof(ProjectAmenity), typeof(BaseProject), "ProjectId", DeleteBehavior.Cascade, false];
        yield return [typeof(ProjectImage), typeof(BaseProject), "ProjectId", DeleteBehavior.Cascade, false];
        yield return [typeof(BaseUnit), typeof(BaseProject), "ProjectId", DeleteBehavior.Cascade, false];
        yield return [typeof(TimePlan), typeof(BaseProject), "ProjectId", DeleteBehavior.Cascade, true];
        yield return [typeof(InitiativeUnit), typeof(BaseUnit), "UnitId", DeleteBehavior.Cascade, true];
        yield return [typeof(UnitAmenity), typeof(BaseUnit), "UnitId", DeleteBehavior.Cascade, false];
        yield return [typeof(UnitImage), typeof(BaseUnit), "UnitId", DeleteBehavior.Cascade, false];
        yield return [typeof(Reservation), typeof(BaseUnit), "UnitId", DeleteBehavior.Restrict, false];
        yield return [typeof(Order), typeof(Reservation), "ReservationId", DeleteBehavior.Restrict, true];
        yield return [typeof(TimePlanStage), typeof(TimePlan), "TimePlanId", DeleteBehavior.Cascade, false];
        yield return [typeof(PlanStageItem), typeof(TimePlanStage), "StageId", DeleteBehavior.Cascade, false];
        yield return [typeof(StageItemImage), typeof(PlanStageItem), "ItemId", DeleteBehavior.Cascade, false];
        yield return [typeof(StageTemplateItem), typeof(StageTemplate), "StageTemplateId", DeleteBehavior.Cascade, false];
        yield return [typeof(StageTemplateItem), typeof(ItemTemplate), "ItemTemplateId", DeleteBehavior.Cascade, false];
    }

    [Theory]
    [MemberData(nameof(Relationships))]
    public void All_FKs_cardinality_and_delete_behaviors_match_the_contract(
        Type dependent, Type principal, string property, DeleteBehavior behavior, bool unique)
    {
        using var context = TestContext.Create();
        var entity = context.Model.FindEntityType(dependent)!;
        var fk = Assert.Single(entity.GetForeignKeys(), fk => fk.Properties.Select(p => p.Name).SequenceEqual([property]));
        Assert.Equal(principal, fk.PrincipalEntityType.ClrType);
        Assert.Equal("Id", Assert.Single(fk.PrincipalKey.Properties).Name);
        Assert.Equal(behavior, fk.DeleteBehavior);
        Assert.Equal(unique, fk.IsUnique);
        Assert.True(fk.IsRequired);
    }

    [Fact]
    public void Every_FK_is_covered_without_redundant_standalone_indexes()
    {
        using var context = TestContext.Create();
        Assert.Equal(22, context.Model.GetEntityTypes().Sum(entity => entity.GetForeignKeys().Count()));

        foreach (var entity in context.Model.GetEntityTypes())
        {
            var indexes = entity.GetIndexes().ToArray();
            foreach (var fk in entity.GetForeignKeys())
            {
                var columns = fk.Properties.Select(property => property.Name).ToArray();
                Assert.True(entity.GetKeys().Any(key => StartsWith(key.Properties, columns)) ||
                    indexes.Any(index => index.GetFilter() is null && StartsWith(index.Properties, columns)),
                    $"Uncovered FK: {entity.ClrType.Name}.{string.Join(",", columns)}");

                var standalone = indexes.SingleOrDefault(index =>
                    index.Properties.Select(property => property.Name).SequenceEqual(columns) && !index.IsUnique);
                if (standalone is not null)
                {
                    Assert.DoesNotContain(indexes, index => index != standalone &&
                        index.GetFilter() is null && StartsWith(index.Properties, columns));
                }
            }
        }
    }

    [Fact]
    public void Shared_initiative_keys_are_PK_FK_and_are_never_generated_independently()
    {
        using var context = TestContext.Create();
        foreach (var type in new[] { typeof(InitiativeProject), typeof(InitiativeUnit) })
        {
            var entity = context.Model.FindEntityType(type)!;
            var property = Assert.Single(entity.FindPrimaryKey()!.Properties);
            Assert.Same(property, Assert.Single(Assert.Single(entity.GetForeignKeys()).Properties));
            Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
            Assert.Empty(entity.GetIndexes());
        }
    }

    [Fact]
    public void No_cascade_path_reaches_reservations()
    {
        using var context = TestContext.Create();
        var entities = context.Model.GetEntityTypes().ToArray();
        foreach (var root in entities)
        {
            var visited = new HashSet<IEntityType>();
            Visit(root);
            void Visit(IEntityType principal)
            {
                if (!visited.Add(principal))
                {
                    return;
                }

                foreach (var fk in principal.GetReferencingForeignKeys().Where(fk => fk.DeleteBehavior == DeleteBehavior.Cascade))
                {
                    Assert.NotEqual(typeof(Reservation), fk.DeclaringEntityType.ClrType);
                    Visit(fk.DeclaringEntityType);
                }
            }
        }
    }

    private static bool StartsWith(IReadOnlyList<IProperty> properties, string[] columns) =>
        properties.Take(columns.Length).Select(property => property.Name).SequenceEqual(columns);
}
