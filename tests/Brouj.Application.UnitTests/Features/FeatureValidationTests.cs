using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Customers.DTOs;
using Brouj.Application.Features.Orders.Commands.CreateFromReservation;
using Brouj.Application.Features.Projects.Commands.ReplaceAmenities;
using Brouj.Application.Features.Projects.DTOs;
using Brouj.Application.Features.Projects.Queries.GetAll;
using Brouj.Application.Features.Reservations.Commands.Create;
using Brouj.Application.Features.StageTemplates.Commands.SetItems;
using Brouj.Application.Features.TimePlans.Commands.ChangeStageItemStatus;
using Brouj.Application.Features.TimePlans.Commands.ReorderStages;
using Brouj.Application.Features.Units.Commands.AddImage;
using Brouj.Application.Features.Units.DTOs;
using Brouj.Application.Features.Users.DTOs;
using Brouj.Application.UnitTests.Features.Support;
using Brouj.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Application.UnitTests.Features;

public sealed class FeatureValidationTests
{
    [Theory]
    [InlineData("01012345678", true)]
    [InlineData("01112345678", true)]
    [InlineData("01212345678", true)]
    [InlineData("01512345678", true)]
    [InlineData("01312345678", false)]
    [InlineData("+201012345678", false)]
    [InlineData("0101234567", false)]
    [InlineData("01012345678\n", false)]
    public void Customer_phone_accepts_only_the_approved_local_Egyptian_mobile_format(string phone, bool valid)
    {
        var input = Profile(phone, "12345678901234");
        var result = new CustomerProfileInputValidator().Validate(input);

        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData("12345678901234", true)]
    [InlineData("1234567890123", false)]
    [InlineData("123456789012345", false)]
    [InlineData("1234567890123X", false)]
    public void National_ID_requires_exactly_fourteen_ASCII_digits(string number, bool valid)
    {
        var result = new CustomerProfileInputValidator().Validate(Profile("01012345678", number));

        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData("       ", false)]
    [InlineData("        ", true)]
    [InlineData("abcdefgh", true)]
    [InlineData("12345678", true)]
    public void New_passwords_have_only_the_eight_character_minimum(string password, bool valid)
    {
        var result = new PasswordChangeInputValidator().Validate(
            new PasswordChangeInput { CurrentPassword = "old", NewPassword = password });

        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    [InlineData(1, 100, true)]
    public void Paging_is_bounded(int number, int size, bool valid)
    {
        var result = new GetProjectsQueryValidator().Validate(new GetProjectsQuery { PageNumber = number, PageSize = size });

        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public void Null_collections_and_null_children_are_validation_failures_instead_of_exceptions()
    {
        var parent = Guid.NewGuid();
        Assert.False(new ReplaceProjectAmenitiesCommandValidator().Validate(
            new ReplaceProjectAmenitiesCommand { ProjectId = parent, Amenities = null! }).IsValid);
        Assert.False(new ReplaceProjectAmenitiesCommandValidator().Validate(
            new ReplaceProjectAmenitiesCommand { ProjectId = parent, Amenities = [null!] }).IsValid);
        Assert.False(new ReorderTimePlanStagesCommandValidator().Validate(
            new ReorderTimePlanStagesCommand { TimePlanId = parent, Items = null! }).IsValid);
        Assert.False(new SetStageTemplateItemsCommandValidator().Validate(
            new SetStageTemplateItemsCommand { Id = parent, Items = [null!] }).IsValid);
    }

    [Fact]
    public void Completion_requires_a_manual_date_but_other_statuses_do_not()
    {
        var id = Guid.NewGuid();
        var validator = new ChangePlanStageItemStatusCommandValidator();
        var completed = validator.Validate(new ChangePlanStageItemStatusCommand
        {
            StageId = id,
            Id = id,
            Status = PlanStageItemStatus.Completed
        });
        var pending = validator.Validate(new ChangePlanStageItemStatusCommand
        {
            StageId = id,
            Id = id,
            Status = PlanStageItemStatus.Pending
        });

        Assert.False(completed.IsValid);
        Assert.Equal("Pls insert the dates first", completed.Errors.Single().ErrorMessage);
        Assert.True(pending.IsValid);
    }

    [Fact]
    public void Calculated_totals_cannot_overflow_or_exceed_the_persisted_order_precision()
    {
        var result = new UnitInputValidator().Validate(new UnitInput
        {
            Number = "oversized",
            Size = 99999999999999m,
            MeterPrice = 999999999999999m
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task An_enormous_page_number_does_not_overflow_the_database_offset()
    {
        using var fixture = new FeatureHarness();
        fixture.Unit();

        var result = await fixture.SendAsync(new GetProjectsQuery { PageNumber = int.MaxValue, PageSize = 100 }, CancellationToken.None);

        Assert.Equal(1, result.Value.TotalCount);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public void Explicit_inputs_exclude_credentials_ownership_totals_and_generated_storage_paths()
    {
        Assert.Equal(new[] { "UnitId" }, typeof(CreateReservationCommand).GetProperties().Select(property => property.Name));
        Assert.Equal(new[] { "ReservationId" }, typeof(CreateOrderFromReservationCommand).GetProperties().Select(property => property.Name));
        Assert.DoesNotContain(typeof(UnitInput).GetProperties(), property =>
            property.Name is "Percentage" or "TotalPrice" or "CustomerId");
        Assert.DoesNotContain(typeof(ProjectInput).GetProperties(), property =>
            property.Name is "IsGarage" or "GaragePrice" or "CreatedAt");
        Assert.DoesNotContain(typeof(AddUnitImageCommand).GetProperties(), property => property.Name == "ImagePath");
        Assert.DoesNotContain(typeof(CustomerDto).GetProperties(), property => property.Name is "IdentityNumber" or "HashPassword");
        Assert.DoesNotContain(typeof(UserDto).GetProperties(), property => property.Name == "HashPassword");
    }

    private static CustomerProfileInput Profile(string phone, string identityNumber) => new()
    {
        FName = "First",
        LName = "Last",
        Email = "valid@example.test",
        Phone = phone,
        IdentityNumber = identityNumber
    };
}
