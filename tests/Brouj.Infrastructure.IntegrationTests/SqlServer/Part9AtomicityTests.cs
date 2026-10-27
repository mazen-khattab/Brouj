using Brouj.Application.Common.Models;
using Brouj.Application.Features.Orders.Commands.CompleteInitiative;
using Brouj.Application.Features.Orders.Commands.CreateFromReservation;
using Brouj.Application.Features.Projects.Commands.Create;
using Brouj.Application.Features.Projects.DTOs;
using Brouj.Application.Features.Reservations.Commands.Create;
using Brouj.Application.Features.TimePlans.Commands.AddStageFromTemplate;
using Brouj.Application.Features.TimePlans.Commands.Create;
using Brouj.Application.Features.TimePlans.Commands.Delete;
using Brouj.Application.Features.TimePlans.DTOs;
using Brouj.Application.Features.Units.Commands.Create;
using Brouj.Application.Features.Units.DTOs;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Brouj.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class Part9AtomicityTests(SqlServerFixture fixture)
{
    [SqlServerTheory]
    [InlineData("InitiativeProject")]
    [InlineData("InitiativeUnit")]
    [InlineData("Reservation")]
    [InlineData("Order")]
    [InlineData("CompleteInitiative")]
    [InlineData("TimePlan")]
    [InlineData("TemplateCopy")]
    [InlineData("DeleteTimePlan")]
    public async Task Failure_after_SQL_writes_rolls_back_the_entire_operation(string operation)
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;

        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, reservations: operation != "Reservation", order: false);
            graph.Project.ProjectType = ProjectType.Initiative;

            if (operation == "CompleteInitiative")
            {
                // A winner and a competing Pending order must both revert after the failing save.
                seed.Orders.AddRange(
                    graph.Order,
                    new Order
                    {
                        ReservationId = graph.OtherReservation.Id,
                        Number = "other",
                        Status = OrderStatus.Pending
                    });
                graph.OtherReservation.UnitId = graph.Unit.Id;
            }

            if (operation == "TimePlan")
            {
                seed.TimePlans.Remove(graph.Plan);
            }

            await seed.SaveChangesAsync();
        }

        await using var baseline = database.CreateContext();
        var before = await SnapshotAsync(baseline);
        var fault = new Part9SaveFailureInterceptor();
        var actor = operation == "Reservation"
            ? new TestActor(true, null, graph.Customer.Id)
            : new TestActor(true, graph.User.Id, null, UserRole.Admin);
        await using var writer = database.CreateContext(actor, enableRetry: true, interceptors: [fault]);
        var files = new Part9FileStorage(writer);
        await using var host = new Part9FeatureHost(writer, actor, files);
        var command = Command(operation, graph);

        // SavedChanges runs after the database performed all writes. The explicit outer transaction
        // must still protect base/initiative rows, template trees, statuses, cascades and audit rows.
        await Assert.ThrowsAsync<InvalidOperationException>(() => host.SendAsync(command));

        Assert.True(fault.WrittenInsideTransaction);
        Assert.Empty(writer.ChangeTracker.Entries());
        Assert.Null(writer.Database.CurrentTransaction);
        Assert.Empty(files.Deleted);

        await using var reader = database.CreateContext();

        Assert.Equal(before, await SnapshotAsync(reader));
    }

    [SqlServerFact]
    public async Task Successful_time_plan_delete_cleans_files_only_after_commit()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;

        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, reservations: false);
        }

        var actor = new TestActor(true, graph.User.Id, null, UserRole.Admin);
        await using var writer = database.CreateContext(actor, enableRetry: true);
        var files = new Part9FileStorage(writer);
        await using var host = new Part9FeatureHost(writer, actor, files);
        var result = await host.SendAsync(new DeleteTimePlanCommand { Id = graph.Plan.Id });

        Assert.True(result.IsSuccess);
        Assert.Equal("item.jpg", Assert.Single(files.Deleted));
        Assert.False(files.DeletedInsideTransaction);

        await using var reader = database.CreateContext();

        Assert.False(await reader.TimePlans.AnyAsync());
        Assert.False(await reader.TimePlanStages.AnyAsync());
        Assert.False(await reader.PlanStageItems.AnyAsync());
        Assert.False(await reader.StageItemImages.AnyAsync());
    }

    private static IRequest<Result<Guid>> Command(string operation, TestGraph graph)
    {
        var stage = new StageDraftInput
        {
            StageTemplateId = graph.StageTemplate.Id,
            DeliveryDate = DateTimeOffset.UtcNow.AddDays(10),
            Items = [new StageItemDraftInput
            {
                ItemTemplateId = graph.ItemTemplate.Id, DueDate = DateTimeOffset.UtcNow.AddDays(5)
            }]
        };

        return operation switch
        {
            "InitiativeProject" => new CreateProjectCommand
            {
                Input = new ProjectInput
                {
                    Name = "Atomic initiative",
                    AreaId = graph.Area.Id,
                    Size = 100,
                    Price = 1000,
                    ProjectType = ProjectType.Initiative,
                    NeighborPrice = 2000,
                    DrawDate = DateTimeOffset.UtcNow
                }
            },
            "InitiativeUnit" => new CreateUnitCommand
            {
                ProjectId = graph.Project.Id,
                Input = new UnitInput { Number = "Atomic unit", Size = 1, MeterPrice = 10, NeighborMeterPrice = 20 }
            },
            "Reservation" => new CreateReservationCommand { UnitId = graph.Unit.Id },
            "Order" => new CreateOrderFromReservationCommand { ReservationId = graph.Reservation.Id },
            "CompleteInitiative" => new CompleteInitiativeOrderCommand { Id = graph.Order.Id },
            "TimePlan" => new CreateTimePlanCommand
            {
                ProjectId = graph.Project.Id,
                Input = new TimePlanInput { Name = "Atomic plan", DeliveryDate = DateTimeOffset.UtcNow.AddMonths(1) },
                Stages = [stage]
            },
            "TemplateCopy" => new AddStageFromTemplateCommand { TimePlanId = graph.Plan.Id, Input = stage },
            "DeleteTimePlan" => new DeleteTimePlanCommand { Id = graph.Plan.Id },
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    private static async Task<string[]> SnapshotAsync(ApplicationDbContext context)
    {
        // Compare row identities AND mutable business values through a fresh context. Counts alone
        // would miss a reservation confirmation, cancelled loser or changed completion timestamp.
        var rows = new List<string>();

        rows.AddRange((await context.BaseProjects.AsNoTracking().Select(row => row.Id).ToListAsync())
            .Select(id => "project:" + id));
        rows.AddRange((await context.InitiativeProjects.AsNoTracking().Select(row => row.ProjectId).ToListAsync())
            .Select(id => "initiative-project:" + id));
        rows.AddRange((await context.BaseUnits.AsNoTracking().Select(row => row.Id).ToListAsync())
            .Select(id => "unit:" + id));
        rows.AddRange((await context.InitiativeUnits.AsNoTracking().Select(row => row.UnitId).ToListAsync())
            .Select(id => "initiative-unit:" + id));
        rows.AddRange((await context.Reservations.AsNoTracking().Select(row => new { row.Id, row.Status }).ToListAsync())
            .Select(row => $"reservation:{row.Id}:{row.Status}"));
        rows.AddRange((await context.Orders.AsNoTracking()
            .Select(row => new { row.Id, row.Status, row.CompletedAt }).ToListAsync())
            .Select(row => $"order:{row.Id}:{row.Status}:{row.CompletedAt:O}"));
        rows.AddRange((await context.TimePlans.AsNoTracking().Select(row => row.Id).ToListAsync())
            .Select(id => "plan:" + id));
        rows.AddRange((await context.TimePlanStages.AsNoTracking().Select(row => row.Id).ToListAsync())
            .Select(id => "stage:" + id));
        rows.AddRange((await context.PlanStageItems.AsNoTracking().Select(row => row.Id).ToListAsync())
            .Select(id => "item:" + id));
        rows.AddRange((await context.StageItemImages.AsNoTracking().Select(row => row.Id).ToListAsync())
            .Select(id => "image:" + id));
        rows.AddRange((await context.ActivityLogs.AsNoTracking().Select(row => row.Id).ToListAsync())
            .Select(id => "audit:" + id));

        return rows.Order(StringComparer.Ordinal).ToArray();
    }
}
