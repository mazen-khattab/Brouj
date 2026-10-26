using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Features.Cities.Commands.Create;
using Brouj.Application.Features.Cities.DTOs;
using Brouj.Application.Features.Cities.Queries.GetAll;
using Brouj.Application.Features.Projects.Commands.Create;
using Brouj.Application.Features.Projects.Commands.Delete;
using Brouj.Application.UnitTests.Features.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Application.UnitTests.Features;

public sealed class CommittedEffectTests
{
    [Fact]
    public async Task Successful_writes_deliver_the_invalidation_intent_and_remove_pending_files_after_commit()
    {
        using var fixture = new FeatureHarness();
        var cleanup = fixture.Services.GetRequiredService<PendingFileCleanup>();
        var publisher = new FeaturePublisher();
        var behavior = new Brouj.Application.Behaviors.CommittedEffectsBehavior<CreateProjectCommand, Result<Guid>>(
            cleanup, publisher, Microsoft.Extensions.Logging.Abstractions.NullLogger<
                Brouj.Application.Behaviors.CommittedEffectsBehavior<CreateProjectCommand, Result<Guid>>>.Instance);
        var committed = false;
        cleanup.Add("generated.png");

        var result = await behavior.Handle(new CreateProjectCommand(), cancellationToken =>
        {
            Assert.Empty(fixture.Files.Deleted);
            Assert.Empty(publisher.Published);
            committed = true;

            return Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));
        }, CancellationToken.None);

        Assert.True(committed && result.IsSuccess);
        Assert.Single(fixture.Files.Deleted);
        var intent = Assert.IsType<Brouj.Application.Common.Notifications.CacheInvalidationRequested>(
            Assert.Single(publisher.Published));
        Assert.Equal("catalogue", intent.Resource);
    }

    [Fact]
    public async Task Failed_transactions_discard_file_cleanup_and_do_not_publish_invalidation()
    {
        using var fixture = new FeatureHarness();
        var cleanup = fixture.Services.GetRequiredService<PendingFileCleanup>();
        var publisher = new FeaturePublisher();
        var behavior = new Brouj.Application.Behaviors.CommittedEffectsBehavior<DeleteProjectCommand, Result<Guid>>(
            cleanup, publisher, Microsoft.Extensions.Logging.Abstractions.NullLogger<
                Brouj.Application.Behaviors.CommittedEffectsBehavior<DeleteProjectCommand, Result<Guid>>>.Instance);
        cleanup.Add("still-referenced.png");

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(new DeleteProjectCommand(),
            cancellationToken => throw new InvalidOperationException("synthetic rollback"), CancellationToken.None));
        await cleanup.CompleteAsync();

        Assert.Empty(fixture.Files.Deleted);
        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task Provider_errors_are_sanitized_before_reaching_host_logs()
    {
        var behavior = new Brouj.Application.Behaviors.FeatureFailureBehavior<CreateProjectCommand, Result<Guid>>();
        var error = await Assert.ThrowsAsync<FeaturePersistenceException>(() => behavior.Handle(
            new CreateProjectCommand(), cancellationToken => throw new DbUpdateException("private identity value"),
            CancellationToken.None));

        Assert.Null(error.InnerException);
        Assert.DoesNotContain("private identity", error.Message);
    }

    [Fact]
    public async Task Handler_database_io_receives_the_request_cancellation_token()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        using var source = new CancellationTokenSource();
        var result = await fixture.SendAsync(new CreateCityCommand { Input = new CityInput { Name = "Test" } }, source.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(source.Token, fixture.Db.LastCancellationToken);

        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.SendAsync(new GetCitiesQuery(), source.Token));
    }
}
