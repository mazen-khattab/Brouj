using Brouj.Application.Common.Services;
using Brouj.Application.Features.Projects.Commands.Create;
using Brouj.Application.Features.Projects.Commands.Delete;
using Brouj.Application.Features.Projects.DTOs;
using Brouj.Application.Features.Projects.Queries.GetById;
using Brouj.Application.Features.TimePlans.Commands.ReorderStages;
using Brouj.Application.Features.TimePlans.DTOs;
using Brouj.Application.Features.TimePlans.Queries.GetByProject;
using Brouj.Application.Features.TimePlans.Services;
using Brouj.Application.Features.Units.Queries.GetById;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Brouj.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class ApplicationFeaturePersistenceTests
{
    private readonly SqlServerFixture _fixture;

    public ApplicationFeaturePersistenceTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task Initiative_project_child_failure_rolls_back_parent_and_audit_rows()
    {
        await using var database = await _fixture.CreateDatabaseAsync();
        await using var seed = database.CreateContext();
        var graph = await TestGraph.SeedAsync(seed, reservations: false);

        // Fixed test-only DDL forces failure after the parent insert, exercising the actual SQL transaction.
        await seed.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER Part7RejectInitiative ON InitiativeProjects AFTER INSERT AS THROW 51007, 'Synthetic child failure', 1;");
        var actor = new TestActor(true, graph.User.Id, null, UserRole.Admin);
        await using var writer = database.CreateContext(actor);
        var handler = new CreateProjectCommandHandler(writer);
        var before = await writer.BaseProjects.CountAsync();

        await Assert.ThrowsAsync<DbUpdateException>(() => ApplicationFeaturePipeline.SendAsync(handler, new CreateProjectCommand
        {
            Input = new ProjectInput
            {
                Name = "Must roll back",
                AreaId = graph.Area.Id,
                Size = 100,
                Price = 1000,
                ProjectType = ProjectType.Initiative,
                NeighborPrice = 2000,
                DrawDate = DateTimeOffset.UtcNow
            }
        }, writer, actor));

        await using var reader = database.CreateContext();

        Assert.Equal(before, await reader.BaseProjects.CountAsync());
        Assert.False(await reader.ActivityLogs.AnyAsync());
    }

    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reorder_swaps_unique_positions_and_rolls_back_temporary_values_on_failure(bool failFinalSave)
    {
        await using var database = await _fixture.CreateDatabaseAsync();
        TestGraph graph;
        Guid secondId;

        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, reservations: false);
            var second = new TimePlanStage
            {
                Id = Guid.NewGuid(),
                TimePlanId = graph.Plan.Id,
                Name = "Second",
                SortOrder = 2,
                DeliveryDate = DateTimeOffset.UtcNow
            };
            seed.TimePlanStages.Add(second);
            await seed.SaveChangesAsync();
            secondId = second.Id;
        }

        var actor = new TestActor(true, graph.User.Id, null, UserRole.Admin);
        await using var writer = failFinalSave
            ? database.CreateContext(actor, interceptors: [new Part7SaveFailureInterceptor()])
            : database.CreateContext(actor);
        var ordering = new TimePlanOrdering(writer);
        var handler = new ReorderTimePlanStagesCommandHandler(writer, ordering);
        var command = new ReorderTimePlanStagesCommand
        {
            TimePlanId = graph.Plan.Id,
            Items = [
                new ReorderItemInput { Id = secondId, SortOrder = 1 },
                new ReorderItemInput { Id = graph.Item.StageId, SortOrder = 2 }
            ]
        };

        if (failFinalSave)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ApplicationFeaturePipeline.SendAsync(handler, command, writer, actor));
        }
        else
        {
            var result = await ApplicationFeaturePipeline.SendAsync(handler, command, writer, actor);
            Assert.True(result.IsSuccess);
        }

        await using var reader = database.CreateContext();
        var rows = await reader.TimePlanStages.AsNoTracking().Where(stage => stage.TimePlanId == graph.Plan.Id)
            .OrderBy(stage => stage.SortOrder).ToArrayAsync();

        Assert.Equal(new[] { 1, 2 }, rows.Select(stage => stage.SortOrder));
        Assert.Equal(failFinalSave ? graph.Item.StageId : secondId, rows[0].Id);
    }

    [SqlServerFact]
    public async Task Feature_projections_translate_on_SQL_Server_and_do_not_track_entities()
    {
        await using var database = await _fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();
        var graph = await TestGraph.SeedAsync(context, reservations: false);
        context.ChangeTracker.Clear();

        var project = await new GetProjectByIdQueryHandler(context)
            .Handle(new GetProjectByIdQuery { Id = graph.Project.Id }, CancellationToken.None);
        var unit = await new GetUnitByIdQueryHandler(context)
            .Handle(new GetUnitByIdQuery { ProjectId = graph.Project.Id, Id = graph.Unit.Id }, CancellationToken.None);
        var actor = new TestActor(true, null, graph.Customer.Id);
        var plan = await ApplicationFeaturePipeline.SendAsync(new GetProjectTimePlanQueryHandler(context),
            new GetProjectTimePlanQuery { ProjectId = graph.Project.Id }, context, actor);

        Assert.True(project.IsSuccess && unit.IsSuccess && plan.IsSuccess);
        Assert.Single(project.Value.Amenities);
        Assert.Single(project.Value.Images);
        Assert.Single(unit.Value.Images);
        Assert.Single(Assert.Single(Assert.Single(plan.Value.Stages).Items).Images);
        Assert.Equal(500m, unit.Value.TotalPrice);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [SqlServerFact]
    public async Task Project_delete_handler_removes_the_approved_tree_without_removing_templates()
    {
        await using var database = await _fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();
        var graph = await TestGraph.SeedAsync(context, reservations: false);
        context.ChangeTracker.Clear();
        var actor = new TestActor(true, graph.User.Id, null, UserRole.Admin);
        var cleanup = new PendingFileCleanup(new Part7FileStorage(), NullLogger<PendingFileCleanup>.Instance);
        var result = await ApplicationFeaturePipeline.SendAsync(new DeleteProjectCommandHandler(context, cleanup),
            new DeleteProjectCommand { Id = graph.Project.Id }, context, actor);

        Assert.True(result.IsSuccess);

        await using var reader = database.CreateContext();

        Assert.False(await reader.BaseProjects.AnyAsync());
        Assert.False(await reader.BaseUnits.AnyAsync());
        Assert.False(await reader.TimePlans.AnyAsync());
        Assert.False(await reader.TimePlanStages.AnyAsync());
        Assert.False(await reader.PlanStageItems.AnyAsync());
        Assert.False(await reader.StageItemImages.AnyAsync());
        Assert.True(await reader.StageTemplates.AnyAsync());
        Assert.True(await reader.ItemTemplates.AnyAsync());
    }
}
