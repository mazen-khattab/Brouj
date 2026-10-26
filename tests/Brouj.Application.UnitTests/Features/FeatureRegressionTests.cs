using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.ActivityLogs.DTOs;
using Brouj.Application.Features.Areas.DTOs;
using Brouj.Application.Features.Cities.DTOs;
using Brouj.Application.Features.Customers.Commands.Restore;
using Brouj.Application.Features.Customers.DTOs;
using Brouj.Application.Features.ItemTemplates.DTOs;
using Brouj.Application.Features.Orders.DTOs;
using Brouj.Application.Features.Projects.Commands.Update;
using Brouj.Application.Features.Projects.DTOs;
using Brouj.Application.Features.Reservations.DTOs;
using Brouj.Application.Features.StageTemplates.DTOs;
using Brouj.Application.Features.Terms.DTOs;
using Brouj.Application.Features.TimePlans.DTOs;
using Brouj.Application.Features.Units.Commands.Update;
using Brouj.Application.Features.Units.DTOs;
using Brouj.Application.Features.Users.Commands.Restore;
using Brouj.Application.Features.Users.DTOs;
using Brouj.Application.UnitTests.Features.Support;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Application.UnitTests.Features;

public sealed class FeatureRegressionTests
{
    [Fact]
    public async Task Project_price_changes_recalculate_numeric_percentages_before_saving()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var unit = fixture.Unit(ProjectType.Initiative);
        var initiative = new InitiativeUnit { UnitId = unit.Id, Unit = unit, Percentage = 10, NeighborMeterPrice = 20 };
        fixture.Db.InitiativeUnits.Add(initiative);
        var valid = await fixture.SendAsync(new UpdateProjectCommand
        {
            Id = unit.ProjectId,
            Input = new ProjectInput
            {
                Name = "Project",
                AreaId = unit.Project.AreaId,
                ProjectType = ProjectType.Initiative,
                Size = 100,
                Price = 2000,
                NeighborPrice = 3000,
                DrawDate = fixture.Clock.GetUtcNow()
            }
        }, CancellationToken.None);
        var invalid = await fixture.SendAsync(new UpdateProjectCommand
        {
            Id = unit.ProjectId,
            Input = new ProjectInput
            {
                Name = "Invalid",
                AreaId = unit.Project.AreaId,
                ProjectType = ProjectType.Initiative,
                Size = 100,
                Price = 50,
                NeighborPrice = 3000,
                DrawDate = fixture.Clock.GetUtcNow()
            }
        }, CancellationToken.None);

        Assert.True(valid.IsSuccess && invalid.IsFailure);
        Assert.Equal(5m, initiative.Percentage);
        Assert.Equal(2000m, unit.Project.Price);
        Assert.Equal("Project", unit.Project.Name);
        Assert.Equal(1, fixture.Db.SaveCount);
    }

    [Fact]
    public async Task A_project_can_change_type_before_any_units_exist()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var unit = fixture.Unit();
        fixture.Db.BaseUnits.Remove(unit);

        var result = await fixture.SendAsync(new UpdateProjectCommand
        {
            Id = unit.ProjectId,
            Input = new ProjectInput
            {
                Name = "Converted",
                AreaId = unit.Project.AreaId,
                ProjectType = ProjectType.Initiative,
                Size = 100,
                Price = 2000,
                NeighborPrice = 3000,
                DrawDate = fixture.Clock.GetUtcNow()
            }
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProjectType.Initiative, unit.Project.ProjectType);
        Assert.Equal(unit.ProjectId, fixture.Db.InitiativeProjects.Single().ProjectId);
    }

    [Fact]
    public async Task Restoring_legacy_accounts_rejects_active_contact_collisions_without_changing_deleted_state()
    {
        using var fixture = new FeatureHarness();
        var customer = fixture.Customer();
        customer.DeletedAt = fixture.Clock.GetUtcNow();
        fixture.Db.Customers.Add(new Customer
        {
            Id = Guid.NewGuid(),
            Email = customer.Email,
            Phone = "01112345678",
            IdentityNumber = "99999999999999"
        });
        var staff = fixture.Staff();
        var deletedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = staff.Email,
            Phone = "01212345678",
            DeletedAt = fixture.Clock.GetUtcNow()
        };
        fixture.Db.Users.Add(deletedUser);

        var customerResult = await fixture.SendAsync(new RestoreCustomerCommand { Id = customer.Id }, CancellationToken.None);
        var userResult = await fixture.SendAsync(new RestoreUserCommand { Id = deletedUser.Id }, CancellationToken.None);

        Assert.True(customerResult.IsFailure && userResult.IsFailure);
        Assert.NotNull(customer.DeletedAt);
        Assert.NotNull(deletedUser.DeletedAt);
        Assert.Equal(0, fixture.Db.SaveCount);
    }

    [Fact]
    public async Task A_unit_update_cannot_move_another_projects_unit_through_a_nested_route()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var first = fixture.Unit();
        var other = fixture.Unit();

        var result = await fixture.SendAsync(new UpdateUnitCommand
        {
            ProjectId = first.ProjectId,
            Id = other.Id,
            Input = new UnitInput { Number = "Injected", Size = 5, MeterPrice = 5 }
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("U1", other.Number);
        Assert.Equal(0, fixture.Db.SaveCount);
    }
}
