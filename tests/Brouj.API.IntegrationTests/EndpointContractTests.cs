using System.Net;
using System.Reflection;
using System.Text.Json;
using Brouj.API.Controllers;
using Brouj.API.Extensions;
using Brouj.API.IntegrationTests.Support;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.IntegrationTests;

/// <summary>Exercises every documented route through HTTP; dispatch recording verifies transport wiring only.</summary>
public sealed class EndpointContractTests : IClassFixture<Part8ApiFactory>
{
    private readonly Part8ApiFactory _factory;

    public EndpointContractTests(Part8ApiFactory factory)
    {
        _factory = factory;
    }

    public static IEnumerable<object[]> Endpoints() => HttpTestRequests.Contracts.Select(endpoint => new object[] { endpoint.Request });

    public static IEnumerable<object[]> ProtectedEndpoints() => HttpTestRequests.Contracts
        .Where(endpoint => endpoint.Policy != "Anonymous").Select(endpoint => new object[] { endpoint.Request });

    [Fact]
    public void Every_approved_request_has_exactly_one_documented_controller_action()
    {
        var requests = typeof(Result).Assembly.GetTypes().Where(type => type.GetInterfaces().Any(contract =>
            contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>))).ToArray();
        var actions = typeof(ApiControllerBase).Assembly.GetTypes().SelectMany(type => type.GetMethods())
            .Select(method => method.GetCustomAttribute<EndpointContractAttribute>()).OfType<EndpointContractAttribute>();

        Assert.Equal(101, requests.Length);
        Assert.Equal(requests.Select(type => type.Name).Order(), actions.Select(action => action.RequestType.Name).Order());
        Assert.Equal(requests.Select(type => type.Name).Order(), HttpTestRequests.Contracts.Select(item => item.Request).Order());
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Route_dispatches_the_correct_request_and_matches_its_documented_contract(string requestName)
    {
        var endpoint = HttpTestRequests.Contracts.Single(item => item.Request == requestName);
        var method = typeof(ApiControllerBase).Assembly.GetTypes().SelectMany(type => type.GetMethods())
            .Single(method => method.GetCustomAttribute<EndpointContractAttribute>()?.RequestType.Name == requestName);
        Assert.Equal(endpoint.RateLimit, method.GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName);
        Assert.Equal(endpoint.Policy == "Anonymous" ? null : endpoint.Policy,
            method.GetCustomAttribute<AuthorizeAttribute>()?.Policy);
        Assert.Equal(endpoint.SuccessStatus, method.GetCustomAttribute<ProducesResponseTypeAttribute>()!.StatusCode);
        var responseType = HttpTestRequests.Resolve(requestName).GetInterfaces()
            .Single(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequest<>))
            .GenericTypeArguments[0].GenericTypeArguments[0];
        Assert.Equal(typeof(ApiResponse<>).MakeGenericType(responseType),
            method.GetCustomAttribute<ProducesResponseTypeAttribute>()!.Type);
        Assert.Equal(endpoint.Body, method.GetParameters().SingleOrDefault(parameter =>
            parameter.GetCustomAttribute<FromBodyAttribute>() is not null ||
            parameter.GetCustomAttribute<FromFormAttribute>() is not null)?.ParameterType.Name);
        Assert.Equal(endpoint.Query, method.GetParameters().SingleOrDefault(parameter =>
            parameter.GetCustomAttribute<FromQueryAttribute>() is not null)?.ParameterType.Name);

        using var client = _factory.CreateHttpsClient();
        using var request = HttpTestRequests.Create(endpoint);
        await HttpTestRequests.AddProofAsync(client, request, _factory.Credential(endpoint.Policy));
        _factory.Recording.LastRequest = null;

        using var response = await client.SendAsync(request);

        Assert.Equal(endpoint.SuccessStatus, (int)response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal(requestName, _factory.Recording.LastRequest!.GetType().Name);
        Assert.True(_factory.Recording.LastCancellationToken.CanBeCanceled);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.True(json.RootElement.TryGetProperty("data", out _));

        // Every path identifier must survive binding independently of query/body values.
        foreach (var parameter in method.GetParameters().Where(parameter =>
            parameter.GetCustomAttribute<FromRouteAttribute>() is not null))
        {
            var value = HttpTestRequests.RouteId(parameter.Name!);
            var dispatchedIds = _factory.Recording.LastRequest.GetType().GetProperties()
                .Where(property => property.PropertyType == typeof(Guid))
                .Select(property => property.GetValue(_factory.Recording.LastRequest));
            Assert.Contains(value, dispatchedIds);
        }
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task Every_protected_route_rejects_anonymous_and_wrong_realm_callers(string requestName)
    {
        var endpoint = HttpTestRequests.Contracts.Single(item => item.Request == requestName);

        using var client = _factory.CreateHttpsClient();
        using var anonymous = HttpTestRequests.Create(endpoint);
        await HttpTestRequests.AddProofAsync(client, anonymous, "");
        using var unauthenticated = await client.SendAsync(anonymous);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);

        if (endpoint.Policy == "MemberOnly")
        {
            return;
        }

        using var wrongActor = HttpTestRequests.Create(endpoint);
        var actor = endpoint.Policy == "CustomerOnly" ? "AdminOnly" : "CustomerOnly";
        await HttpTestRequests.AddProofAsync(client, wrongActor, _factory.Credential(actor));
        using var forbidden = await client.SendAsync(wrongActor);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Application_failures_map_consistently_for_every_controller_action(string requestName)
    {
        var endpoint = HttpTestRequests.Contracts.Single(item => item.Request == requestName);
        var errors = new (Error Error, int Status)[]
        {
            (Error.Validation(FeatureErrorCode.AccessDenied, "Invalid input."), 400),
            (ApplicationErrors.Unauthenticated, 401),
            (ApplicationErrors.Forbidden, 403),
            (Error.NotFound(FeatureErrorCode.AccessDenied, "Not found."), 404),
            (Error.Conflict(FeatureErrorCode.AccessDenied, "Conflict."), 409),
            (Error.Failure(FeatureErrorCode.AccessDenied, "private diagnostic"), 500)
        };
        using var client = _factory.CreateHttpsClient();

        try
        {
            foreach (var (error, status) in errors)
            {
                using var request = HttpTestRequests.Create(endpoint);
                await HttpTestRequests.AddProofAsync(client, request, _factory.Credential(endpoint.Policy));
                _factory.Recording.Error = error;
                using var response = await client.SendAsync(request);

                Assert.Equal(status, (int)response.StatusCode);
                Assert.DoesNotContain("private diagnostic", await response.Content.ReadAsStringAsync());
            }
        }
        finally
        {
            _factory.Recording.Error = null;
        }
    }
}
