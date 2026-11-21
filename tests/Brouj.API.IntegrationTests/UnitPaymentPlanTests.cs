using System.Net;
using System.Net.Http.Json;
using Brouj.API.IntegrationTests.Support;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Units.DTOs;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;

namespace Brouj.API.IntegrationTests;

public sealed class UnitPaymentPlanTests
{
    [Fact]
    public async Task Creation_and_public_detail_keep_plans_separate_and_ordered()
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var project = AddProject(factory, ProjectType.Regular);
        var first = await CreateAsync(factory, client, project, "U1", [
            Installment(3, 66.6667m, 0),
            Installment(1, 33.3333m, 0, "  دفعة مقدمة  ")
        ]);
        var second = await CreateAsync(factory, client, project, "U2", [Installment(1, 100m, 0)]);

        var firstPlan = (await GetAsync(client, first, project.Id)).PaymentPlan;
        Assert.Equal([1, 3], firstPlan.Select(row => row.InstallmentNumber));
        Assert.Equal("دفعة مقدمة", firstPlan[0].InstallmentName);
        Assert.Equal(33.3333m, firstPlan[0].InstallmentPercentage);
        Assert.Equal(0, firstPlan[0].InstallmentPeriodMonths);
        Assert.Equal(100m, Assert.Single((await GetAsync(client, second, project.Id)).PaymentPlan).InstallmentPercentage);

        using var wrongProject = await client.GetAsync(DetailRoute(first, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, wrongProject.StatusCode);
    }

    [Theory]
    [InlineData(95, "percentages must total 100%. The submitted total is 95%.")]
    [InlineData(105, "percentages must total 100%. The submitted total is 105%.")]
    public async Task Incorrect_totals_return_the_total_and_save_nothing(int total, string message)
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var project = AddProject(factory, ProjectType.Regular);

        using var response = await WriteAsync(factory, client, HttpMethod.Post, CreateRoute(project), new
        {
            number = "U1",
            size = 10,
            meterPrice = 100,
            paymentPlan = new[] { Installment(1, 50, 0), Installment(2, total - 50, 3) }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(message, await response.Content.ReadAsStringAsync());
        Assert.Empty(factory.Database.BaseUnits);
        Assert.Empty(factory.Database.UnitPaymentInstallments);
    }

    [Fact]
    public async Task Legacy_plan_can_be_assigned_and_replaced_and_invalid_edits_preserve_it()
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var project = AddProject(factory, ProjectType.Regular);
        var unit = AddUnit(factory, project);
        Assert.Empty((await GetAsync(client, unit.Id, project.Id)).PaymentPlan);

        using var first = await WriteAsync(factory, client, HttpMethod.Put, PlanRoute(unit.Id),
            new { paymentPlan = new[] { Installment(1, 40, 0), Installment(2, 60, 6) } }, "SuperAdminOnly");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var retainedId = factory.Database.UnitPaymentInstallments.Single(row => row.InstallmentNumber == 1).Id;

        using var replacement = await WriteAsync(factory, client, HttpMethod.Put, PlanRoute(unit.Id),
            new { paymentPlan = new[] { Installment(1, 60, 0), Installment(3, 40, 6) } });
        Assert.Equal(HttpStatusCode.OK, replacement.StatusCode);
        Assert.Equal(retainedId, factory.Database.UnitPaymentInstallments.Single(row => row.InstallmentNumber == 1).Id);

        foreach (var plan in new UnitPaymentInstallmentInput?[]?[]
        {
            null, [], [null], [Installment(1, 100, null)], [Installment(1, 100, -1)],
            [Installment(1, 100, 0, " ")], [Installment(1, 39.99999m, 0), Installment(2, 60.00001m, 3)],
            [Installment(1, 50, 0), Installment(1, 50, 3)], [Installment(1, 99, 0)]
        })
        {
            using var invalid = await WriteAsync(factory, client, HttpMethod.Put, PlanRoute(unit.Id), new { paymentPlan = plan });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }

        var saved = (await GetAsync(client, unit.Id, project.Id)).PaymentPlan;
        Assert.Equal([1, 3], saved.Select(row => row.InstallmentNumber));
        Assert.Equal([60m, 40m], saved.Select(row => row.InstallmentPercentage));
    }

    [Fact]
    public async Task Regular_creation_requires_a_plan_and_initiative_routes_reject_plans()
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var regular = AddProject(factory, ProjectType.Regular);
        var initiative = AddProject(factory, ProjectType.Initiative);
        using var missing = await WriteAsync(factory, client, HttpMethod.Post, CreateRoute(regular),
            new { number = "U1", size = 10, meterPrice = 100 });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        var plan = new[] { Installment(1, 100, 0) };
        using var initiativeCreate = await WriteAsync(factory, client, HttpMethod.Post, CreateRoute(initiative),
            new { number = "U1", size = 10, meterPrice = 100, neighborMeterPrice = 110, paymentPlan = plan });
        Assert.Equal(HttpStatusCode.BadRequest, initiativeCreate.StatusCode);

        using var allowed = await WriteAsync(factory, client, HttpMethod.Post, CreateRoute(initiative),
            new { number = "U1", size = 10, meterPrice = 100, neighborMeterPrice = 110, paymentPlan = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
        var unit = Assert.Single(factory.Database.BaseUnits);
        Assert.Empty((await GetAsync(client, unit.Id, initiative.Id)).PaymentPlan);

        using var initiativeEdit = await WriteAsync(factory, client, HttpMethod.Put, PlanRoute(unit.Id), new { paymentPlan = plan });
        Assert.Equal(HttpStatusCode.BadRequest, initiativeEdit.StatusCode);
        Assert.Empty(factory.Database.UnitPaymentInstallments);

        using var missingUnit = await WriteAsync(factory, client, HttpMethod.Put, PlanRoute(Guid.NewGuid()), new { paymentPlan = plan });
        Assert.Equal(HttpStatusCode.NotFound, missingUnit.StatusCode);
    }

    private static UnitPaymentInstallmentInput Installment(int number, decimal percentage, int? months, string name = "Installment") =>
        new()
        {
            InstallmentName = name,
            InstallmentNumber = number,
            InstallmentPercentage = percentage,
            InstallmentPeriodMonths = months
        };

    private static BaseProject AddProject(Part8ApiFactory factory, ProjectType type)
    {
        var project = new BaseProject { Id = Guid.NewGuid(), ProjectType = type, Price = 10_000 };
        factory.Database.BaseProjects.Add(project);
        return project;
    }

    private static BaseUnit AddUnit(Part8ApiFactory factory, BaseProject project)
    {
        var unit = new BaseUnit { Id = Guid.NewGuid(), ProjectId = project.Id };
        factory.Database.BaseUnits.Add(unit);
        return unit;
    }

    private static async Task<Guid> CreateAsync(Part8ApiFactory factory, HttpClient client, BaseProject project,
        string number, UnitPaymentInstallmentInput[] plan)
    {
        using var response = await WriteAsync(factory, client, HttpMethod.Post, CreateRoute(project),
            new { number, size = 10, meterPrice = 100, paymentPlan = plan });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>())!.Data;
    }

    private static async Task<UnitDto> GetAsync(HttpClient client, Guid unitId, Guid projectId) =>
        (await client.GetFromJsonAsync<ApiResponse<UnitDto>>(DetailRoute(unitId, projectId)))!.Data;

    private static async Task<HttpResponseMessage> WriteAsync(Part8ApiFactory factory, HttpClient client,
        HttpMethod method, string route, object body, string actor = "AdminOnly")
    {
        using var request = new HttpRequestMessage(method, route) { Content = JsonContent.Create(body) };
        await HttpTestRequests.AddProofAsync(client, request, factory.Credential(actor));
        return await client.SendAsync(request);
    }

    private static string CreateRoute(BaseProject project) => $"/api/v1/admin/projects/{project.Id}/units";
    private static string PlanRoute(Guid unitId) => $"/api/v1/admin/units/{unitId}/payment-plan";
    private static string DetailRoute(Guid unitId, Guid projectId) => $"/api/v1/units/{unitId}?projectId={projectId}";
}
