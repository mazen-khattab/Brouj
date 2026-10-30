using Brouj.Application.Common.DTOs;
using Brouj.Application.Features.Areas.Queries.GetLookup;
using Brouj.Application.Features.Cities.Queries.GetLookup;
using Brouj.Application.Features.Projects.Queries.GetLookup;
using Brouj.Application.UnitTests.Features.Support;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;

namespace Brouj.Application.UnitTests.Features;

public sealed class LookupFeatureTests
{
    private sealed class CustomLookupDto : LookupDto
    {
        public string Extra { get; init; } = string.Empty;
    }

    [Fact]
    public void LookupDto_supports_inheritance_and_properties()
    {
        var id = Guid.NewGuid();
        var dto = new CustomLookupDto
        {
            Id = id,
            Name = "Test Name",
            Extra = "Additional Field"
        };

        Assert.Equal(id, dto.Id);
        Assert.Equal("Test Name", dto.Name);
        Assert.Equal("Additional Field", dto.Extra);
    }

    [Fact]
    public async Task Cities_lookup_returns_projected_data_and_filters_by_search()
    {
        using var fixture = new FeatureHarness();
        var cairo = new City { Id = Guid.NewGuid(), Name = "Cairo" };
        var alex = new City { Id = Guid.NewGuid(), Name = "Alexandria" };
        var giza = new City { Id = Guid.NewGuid(), Name = "Giza" };
        fixture.Db.Cities.AddRange(cairo, alex, giza);

        var allResult = await fixture.SendAsync(new GetCitiesLookupQuery(), CancellationToken.None);
        Assert.True(allResult.IsSuccess);
        var allList = allResult.Value.ToList();
        Assert.Equal(3, allList.Count);
        Assert.Equal("Alexandria", allList[0].Name);
        Assert.Equal("Cairo", allList[1].Name);
        Assert.Equal("Giza", allList[2].Name);

        var filteredResult = await fixture.SendAsync(new GetCitiesLookupQuery("Cai"), CancellationToken.None);
        Assert.True(filteredResult.IsSuccess);
        var filteredList = filteredResult.Value.ToList();
        var single = Assert.Single(filteredList);
        Assert.Equal(cairo.Id, single.Id);
        Assert.Equal("Cairo", single.Name);
    }

    [Fact]
    public async Task Areas_lookup_returns_projected_data_and_filters_by_search()
    {
        using var fixture = new FeatureHarness();
        var city = new City { Id = Guid.NewGuid(), Name = "Cairo" };
        var maadi = new Area { Id = Guid.NewGuid(), Name = "Maadi", CityId = city.Id };
        var zamalek = new Area { Id = Guid.NewGuid(), Name = "Zamalek", CityId = city.Id };
        fixture.Db.Cities.Add(city);
        fixture.Db.Areas.AddRange(maadi, zamalek);

        var allResult = await fixture.SendAsync(new GetAreasLookupQuery(), CancellationToken.None);
        Assert.True(allResult.IsSuccess);
        var allList = allResult.Value.ToList();
        Assert.Equal(2, allList.Count);

        var filteredResult = await fixture.SendAsync(new GetAreasLookupQuery("Maad"), CancellationToken.None);
        Assert.True(filteredResult.IsSuccess);
        var filteredList = filteredResult.Value.ToList();
        var single = Assert.Single(filteredList);
        Assert.Equal(maadi.Id, single.Id);
        Assert.Equal("Maadi", single.Name);
    }

    [Fact]
    public async Task Projects_lookup_returns_projected_data_and_filters_by_search_and_type()
    {
        using var fixture = new FeatureHarness();
        var city = new City { Id = Guid.NewGuid(), Name = "Cairo" };
        var area = new Area { Id = Guid.NewGuid(), Name = "New Cairo", CityId = city.Id };
        var regularProject = new BaseProject
        {
            Id = Guid.NewGuid(),
            Name = "Brouj Heights",
            AreaId = area.Id,
            ProjectType = ProjectType.Regular
        };
        var initiativeProject = new BaseProject
        {
            Id = Guid.NewGuid(),
            Name = "Brouj Initiative One",
            AreaId = area.Id,
            ProjectType = ProjectType.Initiative
        };
        var otherProject = new BaseProject
        {
            Id = Guid.NewGuid(),
            Name = "Sunrise Compound",
            AreaId = area.Id,
            ProjectType = ProjectType.Regular
        };

        fixture.Db.Cities.Add(city);
        fixture.Db.Areas.Add(area);
        fixture.Db.BaseProjects.AddRange(regularProject, initiativeProject, otherProject);

        // Search only
        var searchResult = await fixture.SendAsync(new GetProjectsLookupQuery("Brouj"), CancellationToken.None);
        Assert.True(searchResult.IsSuccess);
        var searchList = searchResult.Value.ToList();
        Assert.Equal(2, searchList.Count);

        // Search and Type
        var filteredResult = await fixture.SendAsync(
            new GetProjectsLookupQuery("Brouj", ProjectType.Initiative),
            CancellationToken.None);
        Assert.True(filteredResult.IsSuccess);
        var single = Assert.Single(filteredResult.Value);
        Assert.Equal(initiativeProject.Id, single.Id);
        Assert.Equal("Brouj Initiative One", single.Name);

        // Type only
        var typeResult = await fixture.SendAsync(
            new GetProjectsLookupQuery(null, ProjectType.Regular),
            CancellationToken.None);
        Assert.True(typeResult.IsSuccess);
        Assert.Equal(2, typeResult.Value.Count());
    }

    [Fact]
    public void Lookup_validators_enforce_search_length_and_enum_rules()
    {
        var areasValidator = new GetAreasLookupQueryValidator();
        Assert.True(areasValidator.Validate(new GetAreasLookupQuery("valid")).IsValid);
        Assert.False(areasValidator.Validate(new GetAreasLookupQuery(new string('a', 101))).IsValid);

        var citiesValidator = new GetCitiesLookupQueryValidator();
        Assert.True(citiesValidator.Validate(new GetCitiesLookupQuery("valid")).IsValid);
        Assert.False(citiesValidator.Validate(new GetCitiesLookupQuery(new string('a', 101))).IsValid);

        var projectsValidator = new GetProjectsLookupQueryValidator();
        Assert.True(projectsValidator.Validate(new GetProjectsLookupQuery("valid", ProjectType.Regular)).IsValid);
        Assert.False(projectsValidator.Validate(new GetProjectsLookupQuery(new string('a', 101))).IsValid);
        Assert.False(projectsValidator.Validate(new GetProjectsLookupQuery(null, (ProjectType)999)).IsValid);
    }
}
