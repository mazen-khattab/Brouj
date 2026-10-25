using Brouj.Application.Features.ActivityLogs.DTOs;
using Brouj.Application.Features.ActivityLogs.Queries.GetAll;
using Brouj.Application.Features.ActivityLogs.Queries.GetById;
using Brouj.Application.Features.ItemTemplates.Commands.Create;
using Brouj.Application.Features.ItemTemplates.DTOs;
using Brouj.Application.Features.StageTemplates.Commands.Create;
using Brouj.Application.Features.StageTemplates.Commands.SetActive;
using Brouj.Application.Features.StageTemplates.Commands.SetItems;
using Brouj.Application.Features.StageTemplates.DTOs;
using Brouj.Application.Features.Terms.Commands.Activate;
using Brouj.Application.Features.Terms.Commands.CreateVersion;
using Brouj.Application.Features.Terms.DTOs;
using Brouj.Application.Features.Terms.Queries.GetActive;
using Brouj.Application.Features.Terms.Queries.GetVersions;
using Brouj.Application.UnitTests.Features.Support;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Application.UnitTests.Features;

public sealed class HistoryFeatureTests
{
    [Fact]
    public async Task Terms_are_immutable_versions_with_global_number_uniqueness_and_one_active_version()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var first = new TermsAndConditions
        {
            Id = Guid.NewGuid(),
            Title = "First",
            Content = "Original",
            VersionNumber = 1,
            IsActive = true,
            EffectiveDate = fixture.Clock.GetUtcNow().AddDays(-1)
        };
        fixture.Db.TermsAndConditions.Add(first);
        var created = await fixture.SendAsync(new CreateTermsAndConditionsVersionCommand
        {
            Input = new CreateTermsVersionInput
            {
                Title = "Second",
                Content = "New",
                VersionNumber = 2,
                EffectiveDate = fixture.Clock.GetUtcNow()
            }
        }, CancellationToken.None);
        var duplicate = await fixture.SendAsync(new CreateTermsAndConditionsVersionCommand
        {
            Input = new CreateTermsVersionInput
            {
                Title = "Duplicate inactive",
                Content = "other",
                VersionNumber = 2,
                EffectiveDate = fixture.Clock.GetUtcNow()
            }
        }, CancellationToken.None);
        var activated = await fixture.SendAsync(new ActivateTermsAndConditionsCommand { Id = created.Value }, CancellationToken.None);
        var current = await fixture.SendAsync(new GetActiveTermsAndConditionsQuery(), CancellationToken.None);
        var history = await fixture.SendAsync(new GetTermsAndConditionsVersionsQuery { PageSize = 1 }, CancellationToken.None);

        Assert.True(created.IsSuccess && activated.IsSuccess && duplicate.IsFailure);
        Assert.Equal(2, current.Value.VersionNumber);
        Assert.False(first.IsActive);
        Assert.Equal("Original", first.Content);
        Assert.Equal(2, history.Value.TotalCount);
        Assert.Single(fixture.Db.TermsAndConditions, terms => terms.IsActive);
    }

    [Fact]
    public async Task Future_terms_activation_is_rejected_without_changing_the_current_version()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var future = new TermsAndConditions
        {
            Id = Guid.NewGuid(),
            VersionNumber = 5,
            EffectiveDate = fixture.Clock.GetUtcNow().AddMinutes(1)
        };
        fixture.Db.TermsAndConditions.Add(future);

        var result = await fixture.SendAsync(new ActivateTermsAndConditionsCommand { Id = future.Id }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(future.IsActive);
        Assert.Equal(0, fixture.Db.SaveCount);
    }

    [Fact]
    public async Task Template_management_supports_activation_and_validated_mapping_replacement()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var stage = await fixture.SendAsync(new CreateStageTemplateCommand
        {
            Input = new StageTemplateInput { Name = "Stage", Code = "stage" }
        }, CancellationToken.None);
        var item = await fixture.SendAsync(new CreateItemTemplateCommand
        {
            Input = new ItemTemplateInput { Name = "Item", Code = "item" }
        }, CancellationToken.None);
        var active = await fixture.SendAsync(
            new SetStageTemplateActiveCommand { Id = stage.Value, IsActive = true }, CancellationToken.None);
        var mappings = await fixture.SendAsync(
            new SetStageTemplateItemsCommand
            {
                Id = stage.Value,
                Items = [new StageTemplateItemInput { ItemTemplateId = item.Value, SortOrder = 1 }]
            }, CancellationToken.None);
        var missing = await fixture.SendAsync(
            new SetStageTemplateItemsCommand
            {
                Id = stage.Value,
                Items = [new StageTemplateItemInput { ItemTemplateId = Guid.NewGuid(), SortOrder = 1 }]
            }, CancellationToken.None);

        Assert.True(stage.IsSuccess && item.IsSuccess && active.IsSuccess && mappings.IsSuccess);
        Assert.True(missing.IsFailure);
        Assert.Equal(item.Value, fixture.Db.StageTemplateItems.Single().ItemTemplateId);
    }

    [Fact]
    public async Task Activity_logs_apply_all_filters_and_only_project_safe_metadata()
    {
        using var fixture = new FeatureHarness();
        var staff = fixture.Staff(UserRole.Admin);
        var selected = new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = staff.Id,
            EntityId = Guid.NewGuid(),
            EntityType = "Project",
            Action = ActivityAction.Updated,
            CreatedAt = fixture.Clock.GetUtcNow(),
            OldData = "{\"IdentityNumber\":\"private\"}",
            NewData = "{\"HashPassword\":\"private\"}"
        };
        fixture.Db.ActivityLogs.AddRange(selected, new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = staff.Id,
            EntityType = "Other",
            CreatedAt = selected.CreatedAt
        });
        var page = await fixture.SendAsync(new GetActivityLogsQuery
        {
            UserId = staff.Id,
            EntityType = "Project",
            Action = ActivityAction.Updated,
            DateFrom = selected.CreatedAt,
            DateTo = selected.CreatedAt,
            PageSize = 1
        }, CancellationToken.None);
        var detail = await fixture.SendAsync(new GetActivityLogByIdQuery { Id = selected.Id }, CancellationToken.None);

        Assert.Equal(selected.Id, Assert.Single(page.Value.Items).Id);
        Assert.Equal(selected.EntityId, detail.Value.EntityId);
        Assert.DoesNotContain(typeof(ActivityLogDto).GetProperties(), property =>
            property.Name is "OldData" or "NewData");
    }
}
