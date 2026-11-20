using Brouj.Application.Features.Reservations.Commands.Create;
using Brouj.Application.Features.Reservations.Errors;
using Brouj.Application.UnitTests.Features.Support;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;

namespace Brouj.Application.UnitTests.Features;

public sealed class ReservationCreationTests
{
    [Theory]
    [InlineData(null, "25-35", "Cairo", 1, 12)]
    [InlineData("29001011234567", null, "Cairo", 1, 12)]
    [InlineData("29001011234567", "25-35", "   ", 1, 12)]
    [InlineData("29001011234567", "25-35", "Cairo", null, 12)]
    [InlineData("29001011234567", "25-35", "Cairo", 1, null)]
    [InlineData("invalid", "25-35", "Cairo", 1, 12)]
    [InlineData("29001011234567", "age group too long for storage", "Cairo", 1, 12)]
    [InlineData("29001011234567", "25-35", "Cairo", 0, 12)]
    [InlineData("29001011234567", "25-35", "Cairo", 1, -1)]
    public async Task Invalid_initiative_profile_does_not_save_any_reservation(
        string? identity, string? age, string? residence, int? bracket, int? period)
    {
        using var fixture = new FeatureHarness();
        fixture.Customer();
        var unit = fixture.Unit(ProjectType.Initiative);
        var result = await fixture.SendAsync(new CreateReservationCommand
        {
            UnitId = unit.Id, FullName = "Customer", Phone = "01012345678",
            IdentityNumber = identity, AgeGroup = age, PlaceOfResidence = residence,
            PreferredFinancialBracket = bracket, PreferredRepaymentPeriod = period
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ReservationErrors.InitiativeProfileRequired.Code, result.Error.Code);
        Assert.Empty(fixture.Db.Reservations);
        Assert.Equal(0, fixture.Db.SaveCount);
    }

    [Fact]
    public async Task Standard_booking_ignores_initiative_only_fields()
    {
        using var fixture = new FeatureHarness();
        fixture.Customer();
        var unit = fixture.Unit();
        var result = await fixture.SendAsync(new CreateReservationCommand
        {
            UnitId = unit.Id, FullName = "Customer", Phone = "01012345678",
            IdentityNumber = "invalid", PreferredFinancialBracket = -1
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(Assert.Single(fixture.Db.Reservations).InitiativeReservation);
        Assert.Equal(1, fixture.Db.SaveCount);
    }

    [Fact]
    public async Task Valid_profile_is_linked_to_the_reservation_and_saved_once()
    {
        using var fixture = new FeatureHarness();
        fixture.Customer();
        var unit = fixture.Unit(ProjectType.Initiative);
        var result = await fixture.SendAsync(new CreateReservationCommand
        {
            UnitId = unit.Id, FullName = "Customer", Phone = "01012345678",
            IdentityNumber = " 29001011234567 ", AgeGroup = " 25-35 ", PlaceOfResidence = " Cairo ",
            PreferredFinancialBracket = 1, PreferredRepaymentPeriod = 12
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reservation = Assert.Single(fixture.Db.Reservations);
        var profile = Assert.IsType<InitiativeReservation>(reservation.InitiativeReservation);
        Assert.Equal(reservation.Id, profile.ReservationId);
        Assert.Same(reservation, profile.Reservation);
        Assert.Equal("29001011234567", profile.IdentityNumber);
        Assert.Equal("25-35", profile.AgeGroup);
        Assert.Equal("Cairo", profile.PlaceOfResidence);
        Assert.Equal(fixture.Clock.GetUtcNow(), profile.CreatedAt);
        Assert.Equal(1, fixture.Db.SaveCount);
    }
}
