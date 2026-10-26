using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Errors;
using Brouj.Application.Features.Areas.Commands.Create;
using Brouj.Application.Features.Areas.Commands.Delete;
using Brouj.Application.Features.Areas.DTOs;
using Brouj.Application.Features.Cities.Commands.Create;
using Brouj.Application.Features.Cities.Commands.Delete;
using Brouj.Application.Features.Cities.Commands.Update;
using Brouj.Application.Features.Cities.DTOs;
using Brouj.Application.Features.Projects.Commands.AddImage;
using Brouj.Application.Features.Projects.Commands.Create;
using Brouj.Application.Features.Projects.Commands.Delete;
using Brouj.Application.Features.Projects.Commands.ReplaceAmenities;
using Brouj.Application.Features.Projects.Commands.Update;
using Brouj.Application.Features.Projects.Commands.UpdateImage;
using Brouj.Application.Features.Projects.DTOs;
using Brouj.Application.Features.Projects.Queries.GetAll;
using Brouj.Application.Features.Projects.Queries.GetById;
using Brouj.Application.Features.Units.Commands.AddImage;
using Brouj.Application.Features.Units.Commands.Create;
using Brouj.Application.Features.Units.Commands.Delete;
using Brouj.Application.Features.Units.DTOs;
using Brouj.Application.Features.Units.Queries.GetById;
using Brouj.Application.UnitTests.Features.Support;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Application.UnitTests.Features;

public sealed class CatalogueFeatureTests
{
    [Fact]
    public async Task City_and_area_crud_respect_both_delete_restrictions()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var created = await fixture.SendAsync(new CreateCityCommand { Input = new CityInput { Name = "Cairo" } }, CancellationToken.None);
        var area = await fixture.SendAsync(new CreateAreaCommand
        {
            Input = new AreaInput { Name = "Area", CityId = created.Value }
        }, CancellationToken.None);
        var blocked = await fixture.SendAsync(new DeleteCityCommand { Id = created.Value }, CancellationToken.None);
        var updated = await fixture.SendAsync(new UpdateCityCommand { Id = created.Value, Input = new CityInput { Name = "Updated" } }, CancellationToken.None);
        fixture.Db.BaseProjects.Add(new BaseProject { Id = Guid.NewGuid(), AreaId = area.Value });
        var blockedArea = await fixture.SendAsync(new DeleteAreaCommand { Id = area.Value }, CancellationToken.None);

        Assert.True(created.IsSuccess && area.IsSuccess && updated.IsSuccess);
        Assert.True(blocked.IsFailure && blockedArea.IsFailure);
        Assert.Equal("Updated", fixture.Db.Cities.Single().Name);
    }

    [Theory]
    [InlineData(ProjectType.Regular)]
    [InlineData(ProjectType.Initiative)]
    public async Task Project_creation_enforces_server_garage_values_and_initiative_children(ProjectType type)
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var seed = fixture.Unit();
        var result = await fixture.SendAsync(new CreateProjectCommand
        {
            Input = new ProjectInput
            {
                Name = "New",
                AreaId = seed.Project.AreaId,
                Size = 100,
                Price = 1000,
                ProjectType = type,
                NeighborPrice = type == ProjectType.Initiative ? 1500 : null,
                DrawDate = type == ProjectType.Initiative ? fixture.Clock.GetUtcNow() : null
            }
        }, CancellationToken.None);
        var created = fixture.Db.BaseProjects.Single(project => project.Id == result.Value);

        Assert.True(created.IsGarage);
        Assert.Equal(0m, created.GaragePrice);
        Assert.Equal(type == ProjectType.Initiative ? 1 : 0, fixture.Db.InitiativeProjects.Count());
        Assert.Equal(1, fixture.Db.SaveCount);
    }

    [Fact]
    public async Task Unit_percentage_is_derived_stored_as_a_number_and_rejects_values_above_one_hundred()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var seed = fixture.Unit(ProjectType.Initiative);
        var valid = await fixture.SendAsync(new CreateUnitCommand
        {
            ProjectId = seed.ProjectId,
            Input = new UnitInput { Number = "U2", Size = 10, MeterPrice = 10, NeighborMeterPrice = 50 }
        }, CancellationToken.None);
        var invalid = await fixture.SendAsync(new CreateUnitCommand
        {
            ProjectId = seed.ProjectId,
            Input = new UnitInput { Number = "U3", Size = 100, MeterPrice = 100, NeighborMeterPrice = 50 }
        }, CancellationToken.None);

        Assert.True(valid.IsSuccess);
        Assert.True(invalid.IsFailure);
        Assert.Equal(10m, fixture.Db.InitiativeUnits.Single().Percentage);
        Assert.Equal(1, fixture.Db.SaveCount);
    }

    [Fact]
    public async Task Project_type_cannot_change_after_any_unit_exists_and_reserved_trees_cannot_be_deleted()
    {
        using var fixture = new FeatureHarness();
        var customer = fixture.Customer();
        var unit = fixture.Unit();
        fixture.Reserve(unit, customer);
        fixture.Staff();

        var update = await fixture.SendAsync(new UpdateProjectCommand
        {
            Id = unit.ProjectId,
            Input = new ProjectInput
            {
                Name = "Changed",
                AreaId = unit.Project.AreaId,
                Size = 100,
                Price = 1000,
                ProjectType = ProjectType.Initiative,
                NeighborPrice = 1000,
                DrawDate = fixture.Clock.GetUtcNow()
            }
        }, CancellationToken.None);
        var projectDelete = await fixture.SendAsync(new DeleteProjectCommand { Id = unit.ProjectId }, CancellationToken.None);
        var unitDelete = await fixture.SendAsync(new DeleteUnitCommand { ProjectId = unit.ProjectId, Id = unit.Id }, CancellationToken.None);

        Assert.True(update.IsFailure && projectDelete.IsFailure && unitDelete.IsFailure);
        Assert.Equal(0, fixture.Db.SaveCount);
    }

    [Fact]
    public async Task Catalogue_reads_filter_page_and_project_the_required_fields()
    {
        using var fixture = new FeatureHarness();
        var first = fixture.Unit();
        fixture.Unit();
        var result = await fixture.SendAsync(new GetProjectsQuery
        {
            AreaId = first.Project.AreaId,
            CityId = first.Project.Area.CityId,
            Search = "Project",
            ProjectType = ProjectType.Regular,
            PageSize = 1
        }, CancellationToken.None);
        var detail = await fixture.SendAsync(new GetProjectByIdQuery { Id = first.ProjectId }, CancellationToken.None);
        var unit = await fixture.SendAsync(new GetUnitByIdQuery { ProjectId = first.ProjectId, Id = first.Id }, CancellationToken.None);

        Assert.Single(result.Value.Items);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(first.Project.Name, detail.Value.Name);
        Assert.Equal(100m, unit.Value.TotalPrice);
        Assert.True(unit.Value.IsGarage && unit.Value.IsAvailable);
        Assert.Equal(0m, unit.Value.GaragePrice);
        Assert.Equal(0, fixture.Db.SaveCount);
    }

    [Fact]
    public async Task Amenities_are_replaced_and_image_metadata_does_not_accept_a_storage_path()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var unit = fixture.Unit();
        fixture.Db.ProjectAmenities.Add(new ProjectAmenity
        {
            Id = Guid.NewGuid(),
            ProjectId = unit.ProjectId,
            Name = "old",
            Value = "old"
        });
        var amenities = await fixture.SendAsync(
            new ReplaceProjectAmenitiesCommand
            {
                ProjectId = unit.ProjectId,
                Amenities = [new AmenityDto { Name = "Security", Value = "yes" }]
            }, CancellationToken.None);
        var image = await fixture.SendAsync(new AddProjectImageCommand
        {
            ProjectId = unit.ProjectId,
            File = new UploadInput { Content = new MemoryStream([1]), FileName = "untrusted.png", ContentType = "image/png", Length = 1 },
            AltText = "Front"
        }, CancellationToken.None);
        var metadata = await fixture.SendAsync(new UpdateProjectImageCommand
        {
            ProjectId = unit.ProjectId,
            Id = image.Value,
            AltText = "Updated"
        }, CancellationToken.None);

        Assert.True(amenities.IsSuccess && image.IsSuccess && metadata.IsSuccess);
        Assert.Equal("Security", fixture.Db.ProjectAmenities.Single().Name);
        Assert.Equal(fixture.Files.StoredPath, fixture.Db.ProjectImages.Single().ImagePath);
        Assert.Equal("Updated", fixture.Db.ProjectImages.Single().AltText);
    }

    [Fact]
    public async Task Failed_upload_persistence_cleans_the_new_file_without_a_cancelled_token()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var unit = fixture.Unit();
        fixture.Db.FailSave = true;

        await Assert.ThrowsAsync<FeaturePersistenceException>(() => fixture.SendAsync(new AddUnitImageCommand
        {
            UnitId = unit.Id,
            File = new UploadInput
            {
                Content = new MemoryStream([1]),
                FileName = "x.png",
                ContentType = "image/png",
                Length = 1
            }
        }, CancellationToken.None));

        Assert.Equal(fixture.Files.StoredPath, Assert.Single(fixture.Files.Deleted));
        Assert.Equal(CancellationToken.None, fixture.Files.CleanupToken);
    }
}
