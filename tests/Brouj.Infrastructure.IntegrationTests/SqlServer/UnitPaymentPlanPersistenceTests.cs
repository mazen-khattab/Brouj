using Brouj.Application.Features.Projects.Commands.Update;
using Brouj.Application.Features.Projects.DTOs;
using Brouj.Application.Features.Units.Commands.Create;
using Brouj.Application.Features.Units.Commands.ReplacePaymentPlan;
using Brouj.Application.Features.Units.DTOs;
using Brouj.Application.Features.Units.Queries.GetById;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class UnitPaymentPlanPersistenceTests(SqlServerFixture fixture)
{
    [SqlServerFact]
    public async Task Concurrent_replacements_leave_one_complete_plan_and_a_failed_write_preserves_it()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var seed = database.CreateContext();
        var graph = await TestGraph.SeedAsync(seed, reservations: false);
        var actor = new TestActor(true, graph.User.Id, null, UserRole.Admin);
        await using var first = database.CreateContext(actor);
        await using var second = database.CreateContext(actor);
        await using var firstHost = new Part9FeatureHost(first, actor, new Part9FileStorage(first));
        await using var secondHost = new Part9FeatureHost(second, actor, new Part9FileStorage(second));

        var results = await Task.WhenAll(
            firstHost.SendAsync(Replace(graph.Unit.Id, 1, 2)),
            secondHost.SendAsync(Replace(graph.Unit.Id, 1, 3)));
        Assert.All(results, result => Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null));

        await using var reader = database.CreateContext();
        var query = new GetUnitByIdQuery { Id = graph.Unit.Id, ProjectId = graph.Project.Id };
        var handler = new GetUnitByIdQueryHandler(reader);
        var detail = await handler.Handle(query, CancellationToken.None);
        var saved = detail.Value.PaymentPlan.ToArray();
        Assert.Equal(2, saved.Length);
        Assert.Equal(1, saved[0].InstallmentNumber);
        Assert.Contains(saved[1].InstallmentNumber, new[] { 2, 3 });
        Assert.Equal(100m, saved.Sum(row => row.InstallmentPercentage));

        var fault = new Part9SaveFailureInterceptor();
        await using var failingWriter = database.CreateContext(actor, interceptors: [fault]);
        await using var failingHost = new Part9FeatureHost(failingWriter, actor, new Part9FileStorage(failingWriter));
        await Assert.ThrowsAsync<InvalidOperationException>(() => failingHost.SendAsync(Replace(graph.Unit.Id, 4, 5)));
        Assert.True(fault.WrittenInsideTransaction);

        var afterFailure = await handler.Handle(query, CancellationToken.None);
        Assert.Equal(saved.Select(row => row.InstallmentNumber), afterFailure.Value.PaymentPlan.Select(row => row.InstallmentNumber));
        Assert.Equal(saved.Select(row => row.InstallmentPercentage),
            afterFailure.Value.PaymentPlan.Select(row => row.InstallmentPercentage));
    }

    [SqlServerFact]
    public async Task Migrated_table_rejects_duplicate_numbers_and_cascades_on_unit_delete()
    {
        await using var database = await fixture.CreateDatabaseAsync(applyMigrations: true);
        await using var context = database.CreateContext();
        var graph = await TestGraph.SeedAsync(context, reservations: false);
        context.UnitPaymentInstallments.AddRange(
            StoredInstallment(graph.Unit.Id), StoredInstallment(graph.OtherUnit.Id));
        await context.SaveChangesAsync();

        context.UnitPaymentInstallments.Add(StoredInstallment(graph.Unit.Id));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        await context.BaseUnits.Where(unit => unit.Id == graph.Unit.Id).ExecuteDeleteAsync();

        Assert.Equal(graph.OtherUnit.Id, (await context.UnitPaymentInstallments.SingleAsync()).UnitId);
    }

    [SqlServerFact]
    public async Task First_unit_creation_and_project_type_change_cannot_both_succeed()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var seed = database.CreateContext();
        var graph = await TestGraph.SeedAsync(seed, reservations: false);
        var project = new BaseProject
        {
            Id = Guid.NewGuid(),
            AreaId = graph.Area.Id,
            Name = "Empty regular project",
            Size = 100,
            Price = 1000,
            ProjectType = ProjectType.Regular
        };
        seed.BaseProjects.Add(project);
        await seed.SaveChangesAsync();

        var actor = new TestActor(true, graph.User.Id, null, UserRole.Admin);
        await using var creator = database.CreateContext(actor);
        await using var updater = database.CreateContext(actor);
        await using var creatorHost = new Part9FeatureHost(creator, actor, new Part9FileStorage(creator));
        await using var updaterHost = new Part9FeatureHost(updater, actor, new Part9FileStorage(updater));
        var results = await Task.WhenAll(
            creatorHost.SendAsync(new CreateUnitCommand
            {
                ProjectId = project.Id,
                Input = new CreateUnitInput
                {
                    Number = "First", Size = 10, MeterPrice = 10,
                    PaymentPlan = Plan(1, 2)
                }
            }),
            updaterHost.SendAsync(new UpdateProjectCommand
            {
                Id = project.Id,
                Input = new ProjectInput
                {
                    AreaId = project.AreaId, Name = project.Name, Size = 100, Price = 1000,
                    ProjectType = ProjectType.Initiative, NeighborPrice = 1100, DrawDate = DateTimeOffset.UtcNow
                }
            }));

        Assert.Single(results, result => result.IsSuccess);
        await using var reader = database.CreateContext();
        var projectType = await reader.BaseProjects.Where(row => row.Id == project.Id)
            .Select(row => row.ProjectType).SingleAsync();
        var hasUnit = await reader.BaseUnits.AnyAsync(row => row.ProjectId == project.Id);
        Assert.Equal(projectType == ProjectType.Regular, hasUnit);
        Assert.Equal(hasUnit ? 2 : 0, await reader.UnitPaymentInstallments.CountAsync());
    }

    private static ReplaceUnitPaymentPlanCommand Replace(Guid unitId, int first, int second) => new()
    {
        UnitId = unitId,
        Input = new UnitPaymentPlanInput { PaymentPlan = Plan(first, second) }
    };

    private static UnitPaymentInstallmentInput[] Plan(int first, int second) =>
    [
        new() { InstallmentName = "Deposit", InstallmentNumber = first, InstallmentPercentage = 45.25m,
            InstallmentPeriodMonths = 0 },
        new() { InstallmentName = "Balance", InstallmentNumber = second, InstallmentPercentage = 54.75m,
            InstallmentPeriodMonths = 3 }
    ];

    private static UnitPaymentInstallment StoredInstallment(Guid unitId) => new()
    {
        Id = Guid.NewGuid(),
        UnitId = unitId,
        InstallmentName = "Deposit",
        InstallmentNumber = 1,
        InstallmentPercentage = 100,
        InstallmentPeriodMonths = 0
    };
}
