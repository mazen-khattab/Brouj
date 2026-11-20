using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Orders.Commands.CompleteInitiative;
using Brouj.Application.Features.Orders.Commands.CreateFromReservation;
using Brouj.Application.Features.Orders.Queries.GetById;
using Brouj.Application.Features.Orders.Queries.GetMine;
using Brouj.Application.Features.Reservations.Commands.Cancel;
using Brouj.Application.Features.Reservations.Commands.Create;
using Brouj.Application.Features.Reservations.Errors;
using Brouj.Application.Features.Reservations.Queries.GetById;
using Brouj.Application.Features.Reservations.Queries.GetMine;
using Brouj.Application.UnitTests.Features.Support;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Application.UnitTests.Features;

public sealed class CommerceFeatureTests
{
    [Theory]
    [InlineData("10.121", "10.13")]
    [InlineData("10.120", "10.12")]
    [InlineData("0.001", "0.01")]
    public void Totals_always_round_up_to_cents(string raw, string expected)
    {
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture),
            Pricing.Total(1, decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public async Task Reservation_creation_cancellation_and_later_reservation_use_only_the_current_customer()
    {
        using var fixture = new FeatureHarness();
        var customer = fixture.Customer();
        var unit = fixture.Unit();
        var first = await fixture.SendAsync(new CreateReservationCommand { UnitId = unit.Id, FullName = "Customer One", Phone = "01012345678" }, CancellationToken.None);
        var duplicate = await fixture.SendAsync(new CreateReservationCommand { UnitId = unit.Id, FullName = "Customer One", Phone = "01012345678" }, CancellationToken.None);
        var cancellation = await fixture.SendAsync(new CancelReservationCommand { Id = first.Value }, CancellationToken.None);
        var later = await fixture.SendAsync(new CreateReservationCommand { UnitId = unit.Id, FullName = "Customer One", Phone = "01012345678" }, CancellationToken.None);
        var original = fixture.Db.Reservations.Single(reservation => reservation.Id == first.Value);

        Assert.True(first.IsSuccess && cancellation.IsSuccess && later.IsSuccess);
        Assert.True(duplicate.IsFailure);
        Assert.Equal(customer.Id, original.CustomerId);
        Assert.Equal(ReservationStatus.Cancelled, original.Status);
        Assert.Equal(fixture.Clock.GetUtcNow(), original.CancelledAt);
        Assert.NotEqual(first.Value, later.Value);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, true)]
    [InlineData(OrderStatus.Processing, false)]
    [InlineData(OrderStatus.Completed, false)]
    [InlineData(OrderStatus.Cancelled, false)]
    public async Task Only_pending_orders_leave_the_unit_available(OrderStatus status, bool available)
    {
        using var fixture = new FeatureHarness();
        var firstCustomer = fixture.Customer();
        var unit = fixture.Unit(ProjectType.Initiative);
        var previous = fixture.Reserve(unit, firstCustomer);
        previous.Status = ReservationStatus.Confirmed;
        fixture.Db.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            ReservationId = previous.Id,
            Reservation = previous,
            Status = status
        });
        fixture.Customer();

        var result = await fixture.SendAsync(new CreateReservationCommand
        {
            UnitId = unit.Id,
            FullName = "Customer Two",
            Phone = "01012345678",
            IdentityNumber = "29001011234567",
            AgeGroup = "25-35",
            PlaceOfResidence = "Cairo",
            PreferredFinancialBracket = 1,
            PreferredRepaymentPeriod = 12
        }, CancellationToken.None);

        Assert.Equal(available, result.IsSuccess);
    }

    [Fact]
    public async Task Reservations_and_orders_never_expose_another_customers_resources()
    {
        using var fixture = new FeatureHarness();
        var owner = fixture.Customer();
        var unit = fixture.Unit();
        var reservation = fixture.Reserve(unit, owner);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            Reservation = reservation,
            Number = "order"
        };
        fixture.Db.Orders.Add(order);
        fixture.Customer();

        var cancel = await fixture.SendAsync(new CancelReservationCommand { Id = reservation.Id }, CancellationToken.None);
        var detail = await fixture.SendAsync(new GetReservationByIdQuery { Id = reservation.Id }, CancellationToken.None);
        var orderDetail = await fixture.SendAsync(new GetOrderByIdQuery { Id = order.Id }, CancellationToken.None);
        var reservations = await fixture.SendAsync(new GetMyReservationsQuery(), CancellationToken.None);
        var orders = await fixture.SendAsync(new GetMyOrdersQuery(), CancellationToken.None);

        Assert.True(cancel.IsFailure && detail.IsFailure && orderDetail.IsFailure);
        Assert.Empty(reservations.Value.Items);
        Assert.Empty(orders.Value.Items);
        Assert.Equal(0, fixture.Db.SaveCount);
    }

    [Theory]
    [InlineData(ProjectType.Regular, OrderStatus.Processing)]
    [InlineData(ProjectType.Initiative, OrderStatus.Pending)]
    public async Task Staff_order_creation_confirms_the_reservation_and_freezes_the_calculated_total(
        ProjectType projectType,
        OrderStatus expectedStatus)
    {
        using var fixture = new FeatureHarness();
        var customer = fixture.Customer();
        var unit = fixture.Unit(projectType);
        unit.Size = 1;
        unit.MeterPrice = 10.121m;
        var reservation = fixture.Reserve(unit, customer);
        fixture.Staff();

        var result = await fixture.SendAsync(new CreateOrderFromReservationCommand
        {
            ReservationId = reservation.Id
        }, CancellationToken.None);
        var duplicate = await fixture.SendAsync(new CreateOrderFromReservationCommand
        {
            ReservationId = reservation.Id
        }, CancellationToken.None);
        var order = Assert.Single(fixture.Db.Orders);
        unit.MeterPrice = 99;

        Assert.True(result.IsSuccess);
        Assert.True(duplicate.IsFailure);
        Assert.Equal(10.13m, order.TotalPrice);
        Assert.Equal(expectedStatus, order.Status);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public async Task Confirmed_reservations_cannot_be_cancelled_and_customers_cannot_create_orders()
    {
        using var fixture = new FeatureHarness();
        var customer = fixture.Customer();
        var reservation = fixture.Reserve(fixture.Unit(), customer);
        reservation.Status = ReservationStatus.Confirmed;

        var cancel = await fixture.SendAsync(new CancelReservationCommand { Id = reservation.Id }, CancellationToken.None);
        var order = await fixture.SendAsync(new CreateOrderFromReservationCommand { ReservationId = reservation.Id }, CancellationToken.None);

        Assert.True(cancel.IsFailure && order.IsFailure);
    }

    [Fact]
    public async Task Completing_an_initiative_selects_one_winner_and_cancels_every_other_order()
    {
        using var fixture = new FeatureHarness();
        var customer = fixture.Customer();
        var unit = fixture.Unit(ProjectType.Initiative);
        var first = fixture.Reserve(unit, customer);
        var second = fixture.Reserve(unit, fixture.Customer());
        var winner = new Order
        {
            Id = Guid.NewGuid(),
            ReservationId = first.Id,
            Reservation = first,
            Status = OrderStatus.Pending
        };
        var loser = new Order
        {
            Id = Guid.NewGuid(),
            ReservationId = second.Id,
            Reservation = second,
            Status = OrderStatus.Pending
        };
        fixture.Db.Orders.AddRange(winner, loser);
        fixture.Staff();

        var completed = await fixture.SendAsync(new CompleteInitiativeOrderCommand { Id = winner.Id }, CancellationToken.None);
        var otherAttempt = await fixture.SendAsync(new CompleteInitiativeOrderCommand { Id = loser.Id }, CancellationToken.None);

        Assert.True(completed.IsSuccess);
        Assert.True(otherAttempt.IsFailure);
        Assert.Equal(OrderStatus.Completed, winner.Status);
        Assert.Equal(fixture.Clock.GetUtcNow(), winner.CompletedAt);
        Assert.Equal(OrderStatus.Cancelled, loser.Status);
        Assert.Null(loser.CompletedAt);
        Assert.Single(fixture.Db.Orders, order => order.Status == OrderStatus.Completed);
    }

    [Fact]
    public async Task Standard_unit_reservation_succeeds_with_only_name_and_phone()
    {
        using var fixture = new FeatureHarness();
        fixture.Customer();
        var unit = fixture.Unit();

        var result = await fixture.SendAsync(new CreateReservationCommand
        {
            UnitId = unit.Id,
            FullName = "Standard Customer",
            Phone = "01012345678"
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reservation = fixture.Db.Reservations.Single(r => r.Id == result.Value);
        Assert.Equal("Standard Customer", reservation.FullName);
        Assert.Null(reservation.InitiativeReservation);
    }

    [Fact]
    public async Task Initiative_unit_reservation_fails_when_profile_is_missing()
    {
        using var fixture = new FeatureHarness();
        fixture.Customer();
        var unit = fixture.Unit(ProjectType.Initiative);
        var initiativeUnit = new InitiativeUnit
        {
            UnitId = unit.Id,
            NeighborMeterPrice = 100,
            Percentage = 10,
            CreatedAt = fixture.Clock.GetUtcNow()
        };
        unit.InitiativeUnit = initiativeUnit;
        fixture.Db.InitiativeUnits.Add(initiativeUnit);

        var result = await fixture.SendAsync(new CreateReservationCommand
        {
            UnitId = unit.Id,
            FullName = "Initiative Customer",
            Phone = "01012345678"
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ReservationErrors.InitiativeProfileRequired.Code, result.Error.Code);
    }

    [Fact]
    public async Task Initiative_unit_reservation_succeeds_and_creates_initiative_reservation_when_profile_is_supplied()
    {
        using var fixture = new FeatureHarness();
        fixture.Customer();
        var unit = fixture.Unit(ProjectType.Initiative);
        var initiativeUnit = new InitiativeUnit
        {
            UnitId = unit.Id,
            NeighborMeterPrice = 100,
            Percentage = 10,
            CreatedAt = fixture.Clock.GetUtcNow()
        };
        unit.InitiativeUnit = initiativeUnit;
        fixture.Db.InitiativeUnits.Add(initiativeUnit);

        var result = await fixture.SendAsync(new CreateReservationCommand
        {
            UnitId = unit.Id,
            FullName = "Initiative Customer",
            Phone = "01012345678",
            IdentityNumber = "29001011234567",
            AgeGroup = "25-35",
            PlaceOfResidence = "Cairo",
            PreferredFinancialBracket = 1,
            PreferredRepaymentPeriod = 12
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reservation = fixture.Db.Reservations.Single(r => r.Id == result.Value);
        Assert.NotNull(reservation.InitiativeReservation);
        Assert.Equal("29001011234567", reservation.InitiativeReservation.IdentityNumber);
        Assert.Equal("25-35", reservation.InitiativeReservation.AgeGroup);
        Assert.Equal("Cairo", reservation.InitiativeReservation.PlaceOfResidence);
        Assert.Equal(1, reservation.InitiativeReservation.FinancialBracket);
        Assert.Equal(12, reservation.InitiativeReservation.RepaymentPeriod);
    }
}
