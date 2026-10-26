using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Behaviors;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Features.Cities.Commands.Create;
using Brouj.Application.Features.Cities.DTOs;
using Brouj.Application.Features.Cities.Queries.GetAll;
using Brouj.Application.Features.StageTemplates.Commands.SetItems;
using Brouj.Application.UnitTests.Features.Support;
using Brouj.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Application.UnitTests.Behaviors;

public sealed class AuthorizationBehaviorTests
{
    [Theory]
    [InlineData(AccessLevel.Customer, "anonymous", false)]
    [InlineData(AccessLevel.Customer, "customer", true)]
    [InlineData(AccessLevel.Customer, "admin", false)]
    [InlineData(AccessLevel.Customer, "superadmin", false)]
    [InlineData(AccessLevel.Staff, "anonymous", false)]
    [InlineData(AccessLevel.Staff, "customer", false)]
    [InlineData(AccessLevel.Staff, "admin", true)]
    [InlineData(AccessLevel.Staff, "superadmin", true)]
    [InlineData(AccessLevel.SuperAdmin, "anonymous", false)]
    [InlineData(AccessLevel.SuperAdmin, "customer", false)]
    [InlineData(AccessLevel.SuperAdmin, "admin", false)]
    [InlineData(AccessLevel.SuperAdmin, "superadmin", true)]
    [InlineData(AccessLevel.Member, "anonymous", false)]
    [InlineData(AccessLevel.Member, "customer", true)]
    [InlineData(AccessLevel.Member, "admin", true)]
    [InlineData(AccessLevel.Member, "superadmin", true)]
    public async Task Access_levels_preserve_the_approved_actor_matrix(AccessLevel level, string actor, bool allowed)
    {
        using var fixture = new FeatureHarness();

        if (actor == "customer")
        {
            fixture.Customer();
        }
        else if (actor is "admin" or "superadmin")
        {
            fixture.Staff(actor == "admin" ? UserRole.Admin : UserRole.SuperAdmin);
        }

        var behavior = new AuthorizationBehavior<AccessTestRequest, Result<Guid>>(
            fixture.Services.GetRequiredService<FeatureAccess>());
        var handlerCalled = false;

        var result = await behavior.Handle(new AccessTestRequest(level), cancellationToken =>
        {
            handlerCalled = true;

            return Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));
        }, CancellationToken.None);

        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed, handlerCalled);
        Assert.InRange(fixture.Db.AccountReadCount, 0, 1);
    }

    [Fact]
    public async Task A_protected_dispatch_checks_the_account_once_and_rechecks_changed_state_on_the_next_dispatch()
    {
        using var fixture = new FeatureHarness();
        var user = fixture.Staff();
        var command = new CreateCityCommand { Input = new CityInput { Name = "City" } };

        var allowed = await fixture.SendAsync(command, CancellationToken.None);
        user.DeletedAt = fixture.Clock.GetUtcNow();
        var deleted = await fixture.SendAsync(command, CancellationToken.None);
        user.DeletedAt = null;
        user.Role = UserRole.Admin;
        var stale = await fixture.SendAsync(command, CancellationToken.None);
        fixture.Actor.Role = UserRole.Admin;
        var fresh = await fixture.SendAsync(command, CancellationToken.None);

        Assert.True(allowed.IsSuccess && fresh.IsSuccess);
        Assert.True(deleted.IsFailure && stale.IsFailure);
        Assert.Equal(4, fixture.Db.AccountReadCount);
        Assert.Equal(2, fixture.Db.SaveCount);
    }

    [Fact]
    public async Task Public_catalogue_reads_do_not_check_accounts_even_when_an_actor_is_present()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();

        var result = await fixture.SendAsync(new GetCitiesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, fixture.Db.AccountReadCount);
    }

    [Fact]
    public async Task Anonymous_requests_are_rejected_before_invalid_input_or_transaction_work()
    {
        using var fixture = new FeatureHarness();

        var result = await fixture.SendAsync(new SetStageTemplateItemsCommand(), CancellationToken.None);

        Assert.Equal(ApplicationErrors.Unauthenticated, result.Error);
        Assert.Equal(0, fixture.Db.AccountReadCount);
        Assert.Equal(0, fixture.Db.SaveCount);
        Assert.Equal(0, fixture.Transactions.ExecutionCount);
    }

    [Fact]
    public async Task Unknown_access_requirements_fail_closed_without_querying_or_running_the_handler()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var behavior = new AuthorizationBehavior<AccessTestRequest, Result<Guid>>(
            fixture.Services.GetRequiredService<FeatureAccess>());
        var handlerCalled = false;

        var result = await behavior.Handle(new AccessTestRequest((AccessLevel)999), cancellationToken =>
        {
            handlerCalled = true;

            return Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));
        }, CancellationToken.None);

        Assert.Equal(ApplicationErrors.Forbidden, result.Error);
        Assert.False(handlerCalled);
        Assert.Equal(0, fixture.Db.AccountReadCount);
    }

    [Fact]
    public async Task Non_generic_results_preserve_the_same_authorization_error_contract()
    {
        using var fixture = new FeatureHarness();
        var behavior = new AuthorizationBehavior<PlainAccessTestRequest, Result>(
            fixture.Services.GetRequiredService<FeatureAccess>());

        var result = await behavior.Handle(new PlainAccessTestRequest(),
            cancellationToken => throw new InvalidOperationException("The denied handler must not execute."),
            CancellationToken.None);

        Assert.Equal(ApplicationErrors.Unauthenticated, result.Error);
    }

    [Fact]
    public async Task Public_requests_can_return_other_response_types_without_a_failure_factory()
    {
        using var fixture = new FeatureHarness();
        var behavior = new AuthorizationBehavior<PublicAccessTestRequest, string>(
            fixture.Services.GetRequiredService<FeatureAccess>());

        var result = await behavior.Handle(new PublicAccessTestRequest(),
            cancellationToken => Task.FromResult("public"), CancellationToken.None);

        Assert.Equal("public", result);
        Assert.Equal(0, fixture.Db.AccountReadCount);
    }

    [Fact]
    public async Task Cancellation_does_not_start_the_account_query_or_handler()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        using var source = new CancellationTokenSource();
        source.Cancel();
        var behavior = new AuthorizationBehavior<AccessTestRequest, Result<Guid>>(
            fixture.Services.GetRequiredService<FeatureAccess>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.Handle(
            new AccessTestRequest(AccessLevel.Staff),
            cancellationToken => throw new InvalidOperationException("The cancelled handler must not execute."),
            source.Token));

        Assert.Equal(0, fixture.Db.AccountReadCount);
    }

    [Fact]
    public async Task The_handler_receives_the_original_cancellation_token_after_authorization()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        using var source = new CancellationTokenSource();
        var behavior = new AuthorizationBehavior<AccessTestRequest, Result<Guid>>(
            fixture.Services.GetRequiredService<FeatureAccess>());
        var observed = CancellationToken.None;

        await behavior.Handle(new AccessTestRequest(AccessLevel.Staff), cancellationToken =>
        {
            observed = cancellationToken;

            return Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));
        }, source.Token);

        Assert.Equal(source.Token, observed);
        Assert.Equal(1, fixture.Db.AccountReadCount);
    }
}
