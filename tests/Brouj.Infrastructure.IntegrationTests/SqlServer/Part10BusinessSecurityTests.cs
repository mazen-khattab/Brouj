using System.Data.Common;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Cities.Queries.GetAll;
using Brouj.Application.Features.Orders.Commands.CreateFromReservation;
using Brouj.Application.Features.Reservations.Commands.Cancel;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class Part10BusinessSecurityTests(SqlServerFixture fixture)
{
    [SqlServerFact]
    public async Task Customer_cancellation_and_admin_order_creation_cannot_both_succeed()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        TestGraph graph;

        await using (var seed = database.CreateContext())
        {
            graph = await TestGraph.SeedAsync(seed, order: false);
        }

        var rendezvous = new Part9RequestRendezvous();
        var customer = new TestActor(true, null, graph.Customer.Id);
        var admin = new TestActor(true, graph.User.Id, null, UserRole.Admin);

        async Task<Result<Guid>> CancelAsync()
        {
            await using var context = database.CreateContext(customer, enableRetry: true, interceptors: [rendezvous]);

            return await ApplicationFeaturePipeline.SendWithConflictMappingAsync(
                new CancelReservationCommandHandler(context, customer, TimeProvider.System),
                new CancelReservationCommand { Id = graph.Reservation.Id }, context, customer);
        }

        async Task<Result<Guid>> OrderAsync()
        {
            await using var context = database.CreateContext(admin, enableRetry: true, interceptors: [rendezvous]);

            return await ApplicationFeaturePipeline.SendWithConflictMappingAsync(
                new CreateOrderFromReservationCommandHandler(context),
                new CreateOrderFromReservationCommand { ReservationId = graph.Reservation.Id }, context, admin);
        }

        var outcomes = await Task.WhenAll(CancelAsync(), OrderAsync());

        Assert.Equal(2, rendezvous.Arrivals);
        Assert.Single(outcomes, result => result.IsSuccess);
        Assert.Single(outcomes, result => result.IsFailure);

        await using var reader = database.CreateContext();
        var reservation = await reader.Reservations.AsNoTracking().SingleAsync(row => row.Id == graph.Reservation.Id);
        var hasOrder = await reader.Orders.AnyAsync(order => order.ReservationId == graph.Reservation.Id);

        Assert.Equal(hasOrder ? ReservationStatus.Confirmed : ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal(!hasOrder, reservation.CancelledAt is not null);
        Assert.Equal(outcomes[1].IsSuccess, hasOrder);
    }

    [SqlServerTheory]
    [InlineData("' OR 1=1; --")]
    [InlineData("x'; DROP TABLE Users; --")]
    [InlineData("%_ [literal] \\ value")]
    public async Task Search_payloads_remain_literal_parameter_values_in_real_SQL(string payload)
    {
        await using var database = await fixture.CreateDatabaseAsync();

        await using (var seed = database.CreateContext())
        {
            await TestGraph.SeedAsync(seed, reservations: false);
            seed.Cities.Add(new City { Name = payload });
            await seed.SaveChangesAsync();
        }

        var capture = new CaptureSearchCommands();
        await using var context = database.CreateContext(interceptors: [capture]);
        var result = await new GetCitiesQueryHandler(context).Handle(
            new GetCitiesQuery { Search = payload }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(payload, Assert.Single(result.Value.Items).Name);
        Assert.Equal(1, await context.Users.CountAsync());
        Assert.True(capture.ParameterizedSearches >= 2);
        Assert.DoesNotContain(capture.Commands, command => command.Contains(payload, StringComparison.Ordinal));
    }

    private sealed class CaptureSearchCommands : DbCommandInterceptor
    {
        public int ParameterizedSearches { get; private set; }

        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);

            if (command.Parameters.Cast<DbParameter>().Any(parameter => parameter.Value is string))
            {
                ParameterizedSearches++;
            }

            return ValueTask.FromResult(result);
        }
    }
}
