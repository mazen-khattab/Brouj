using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Orders.Commands.CompleteInitiative;
using Brouj.Application.Features.Orders.Commands.CreateFromReservation;
using Brouj.Application.Features.Reservations.Commands.Create;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class Part9ConcurrencyTests(SqlServerFixture fixture)
{
    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Two_admins_creating_orders_have_exactly_one_winner(bool sameReservation)
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var (graph, second, secondAdmin) = await SeedContendersAsync(database);
        var rendezvous = new Part9RequestRendezvous();

        async Task<Result<Guid>> CreateAsync(Guid reservationId, Guid adminId)
        {
            var actor = new TestActor(true, adminId, null, UserRole.Admin);
            await using var writer = database.CreateContext(actor, enableRetry: true, interceptors: [rendezvous]);

            return await ApplicationFeaturePipeline.SendWithConflictMappingAsync(
                new CreateOrderFromReservationCommandHandler(writer),
                new CreateOrderFromReservationCommand { ReservationId = reservationId }, writer, actor);
        }

        var attempts = await Task.WhenAll(
            CreateAsync(graph.Reservation.Id, graph.User.Id),
            CreateAsync(sameReservation ? graph.Reservation.Id : second.Id, secondAdmin.Id));

        Assert.Equal(2, rendezvous.Arrivals);
        Assert.Single(attempts, result => result.IsSuccess);
        Assert.Equal(FeatureErrorCode.OrderConflict, Assert.Single(attempts, result => result.IsFailure).Error.Code);

        await using var reader = database.CreateContext();
        var order = await reader.Orders.AsNoTracking().SingleAsync();
        var reservation = await reader.Reservations.AsNoTracking().SingleAsync(row => row.Id == order.ReservationId);

        Assert.Equal(OrderStatus.Processing, order.Status);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(500m, order.TotalPrice);
        Assert.Equal(1, await reader.Reservations.CountAsync(row => row.UnitId == graph.Unit.Id &&
            row.Status == ReservationStatus.Confirmed));
        Assert.Equal(2, await reader.ActivityLogs.CountAsync());
    }

    [SqlServerFact]
    public async Task Concurrent_active_reservation_requests_return_one_success_and_one_business_conflict()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;

        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, reservations: false);
        }

        var rendezvous = new Part9RequestRendezvous(beforeSave: true);
        var actor = new TestActor(true, null, graph.Customer.Id);

        async Task<Result<Guid>> CreateAsync()
        {
            await using var writer = database.CreateContext(actor, enableRetry: true, interceptors: [rendezvous]);

            return await ApplicationFeaturePipeline.SendWithConflictMappingAsync(
                new CreateReservationCommandHandler(writer, actor, TimeProvider.System),
                new CreateReservationCommand { UnitId = graph.Unit.Id }, writer, actor);
        }

        var attempts = await Task.WhenAll(CreateAsync(), CreateAsync());

        Assert.Equal(2, rendezvous.Arrivals);
        Assert.Single(attempts, result => result.IsSuccess);
        Assert.Equal(FeatureErrorCode.ReservationAlreadyActive,
            Assert.Single(attempts, result => result.IsFailure).Error.Code);

        await using var reader = database.CreateContext();

        Assert.Equal(1, await reader.Reservations.CountAsync(row => row.CustomerId == graph.Customer.Id &&
            row.UnitId == graph.Unit.Id && row.Status == ReservationStatus.Processing));
    }

    [SqlServerTheory]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Confirmed)]
    public async Task Inactive_reservation_does_not_block_a_later_processing_request(ReservationStatus previousStatus)
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;

        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, order: false);
            graph.Reservation.Status = previousStatus;
            await seed.SaveChangesAsync();
        }

        var actor = new TestActor(true, null, graph.Customer.Id);
        await using var writer = database.CreateContext(actor, enableRetry: true);
        var result = await ApplicationFeaturePipeline.SendWithConflictMappingAsync(
            new CreateReservationCommandHandler(writer, actor, TimeProvider.System),
            new CreateReservationCommand { UnitId = graph.Unit.Id }, writer, actor);

        Assert.True(result.IsSuccess);

        await using var reader = database.CreateContext();

        Assert.Equal(1, await reader.Reservations.CountAsync(row => row.CustomerId == graph.Customer.Id &&
            row.UnitId == graph.Unit.Id && row.Status == ReservationStatus.Processing));
        Assert.Equal(previousStatus, (await reader.Reservations.FindAsync(graph.Reservation.Id))!.Status);
    }

    [SqlServerTheory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task Any_existing_regular_unit_order_blocks_another_order(OrderStatus existingStatus)
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var (graph, second, _) = await SeedContendersAsync(database);

        await using (var seed = database.CreateContext())
        {
            seed.Orders.Add(new Order
            {
                ReservationId = graph.Reservation.Id,
                Number = "existing",
                TotalPrice = 500,
                Status = existingStatus
            });
            await seed.SaveChangesAsync();
        }

        var actor = new TestActor(true, graph.User.Id, null, UserRole.Admin);
        await using var writer = database.CreateContext(actor, enableRetry: true);
        var result = await ApplicationFeaturePipeline.SendWithConflictMappingAsync(
            new CreateOrderFromReservationCommandHandler(writer),
            new CreateOrderFromReservationCommand { ReservationId = second.Id }, writer, actor);

        Assert.Equal(FeatureErrorCode.OrderConflict, result.Error.Code);

        await using var reader = database.CreateContext();

        Assert.Equal(1, await reader.Orders.CountAsync());
        Assert.Equal(ReservationStatus.Processing, (await reader.Reservations.FindAsync(second.Id))!.Status);
    }

    [SqlServerFact]
    public async Task Concurrent_initiative_completions_preserve_one_winner()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var (graph, second, secondAdmin) = await SeedContendersAsync(database);
        var firstOrder = new Order { ReservationId = graph.Reservation.Id, Number = "first", Status = OrderStatus.Pending };
        var secondOrder = new Order { ReservationId = second.Id, Number = "second", Status = OrderStatus.Pending };

        await using (var seed = database.CreateContext())
        {
            await seed.BaseProjects.Where(project => project.Id == graph.Project.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(project => project.ProjectType, ProjectType.Initiative));
            seed.Orders.AddRange(firstOrder, secondOrder);
            await seed.SaveChangesAsync();
        }

        var rendezvous = new Part9RequestRendezvous();

        async Task<Result<Guid>> CompleteAsync(Guid orderId, Guid adminId)
        {
            var actor = new TestActor(true, adminId, null, UserRole.Admin);
            await using var writer = database.CreateContext(actor, enableRetry: true, interceptors: [rendezvous]);

            return await ApplicationFeaturePipeline.SendWithConflictMappingAsync(
                new CompleteInitiativeOrderCommandHandler(writer, TimeProvider.System),
                new CompleteInitiativeOrderCommand { Id = orderId }, writer, actor);
        }

        var attempts = await Task.WhenAll(
            CompleteAsync(firstOrder.Id, graph.User.Id), CompleteAsync(secondOrder.Id, secondAdmin.Id));

        Assert.Single(attempts, result => result.IsSuccess);
        Assert.Equal(FeatureErrorCode.OrderConflict, Assert.Single(attempts, result => result.IsFailure).Error.Code);

        await using var reader = database.CreateContext();

        Assert.Equal(1, await reader.Orders.CountAsync(order => order.Status == OrderStatus.Completed &&
            order.CompletedAt != null));
        Assert.Equal(1, await reader.Orders.CountAsync(order => order.Status == OrderStatus.Cancelled &&
            order.CompletedAt == null));
    }

    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Repeated_create_in_a_new_context_returns_conflict_without_duplicate_rows(bool createOrder)
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;

        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, reservations: createOrder, order: false);
        }

        var actor = createOrder
            ? new TestActor(true, graph.User.Id, null, UserRole.Admin)
            : new TestActor(true, null, graph.Customer.Id);

        async Task<Result<Guid>> CreateAsync()
        {
            await using var writer = database.CreateContext(actor, enableRetry: true);

            if (createOrder)
            {
                return await ApplicationFeaturePipeline.SendWithConflictMappingAsync(
                    new CreateOrderFromReservationCommandHandler(writer),
                    new CreateOrderFromReservationCommand { ReservationId = graph.Reservation.Id }, writer, actor);
            }

            return await ApplicationFeaturePipeline.SendWithConflictMappingAsync(
                new CreateReservationCommandHandler(writer, actor, TimeProvider.System),
                new CreateReservationCommand { UnitId = graph.Unit.Id }, writer, actor);
        }

        var first = await CreateAsync();
        var repeated = await CreateAsync();

        Assert.True(first.IsSuccess);
        Assert.Equal(createOrder ? FeatureErrorCode.OrderConflict : FeatureErrorCode.ReservationAlreadyActive,
            repeated.Error.Code);

        await using var reader = database.CreateContext();

        Assert.Equal(1, await reader.Reservations.CountAsync(row => row.UnitId == graph.Unit.Id));
        Assert.Equal(createOrder ? 1 : 0, await reader.Orders.CountAsync());
    }

    private static async Task<(TestGraph Graph, Reservation Second, User Admin)> SeedContendersAsync(TestDatabase database)
    {
        await using var seed = database.CreateContext();
        var graph = await TestGraph.SeedAsync(seed, order: false);
        var suffix = Guid.NewGuid().ToString("N");
        var customer = new Customer
        {
            FName = "Other",
            LName = "Customer",
            Email = suffix + "@example.test",
            Phone = suffix,
            IdentityNumber = suffix,
            HashPassword = "synthetic-hash"
        };
        var admin = new User
        {
            Name = "Other admin",
            Email = suffix + "@staff.test",
            Phone = suffix,
            HashPassword = "synthetic-hash",
            Role = UserRole.Admin
        };
        var reservation = new Reservation
        {
            UnitId = graph.Unit.Id,
            Customer = customer,
            Number = "contender",
            Status = ReservationStatus.Processing,
            ReservationDate = DateTimeOffset.UtcNow
        };

        seed.AddRange(customer, admin, reservation);
        await seed.SaveChangesAsync();

        return (graph, reservation, admin);
    }
}
