using Brouj.Application.Common.Services;
using Brouj.Application.Features.TimePlans.Commands.ChangeStageItemStatus;
using Brouj.Application.Features.TimePlans.Commands.Create;
using Brouj.Application.Features.TimePlans.Commands.DeleteStageItem;
using Brouj.Application.Features.TimePlans.Commands.ReorderStages;
using Brouj.Application.Features.TimePlans.DTOs;
using Brouj.Application.Features.TimePlans.Queries.GetByProject;
using Brouj.Application.UnitTests.Features.Support;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Application.UnitTests.Features;

public sealed class PlanningFeatureTests
{
    [Fact]
    public async Task Multiple_selected_templates_save_as_independent_snapshots_with_manual_dates()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var unit = fixture.Unit();
        var source = SeedTemplate(fixture);
        var due = fixture.Clock.GetUtcNow().AddDays(15);
        var confirmed = due.AddDays(30);
        var command = new CreateTimePlanCommand
        {
            ProjectId = unit.ProjectId,
            Input = new TimePlanInput { Name = "Plan", DeliveryDate = due },
            Stages = Enumerable.Range(1, 2).Select(index => new StageDraftInput
            {
                StageTemplateId = source.Id,
                DeliveryDate = due.AddDays(-5),
                Items = [new StageItemDraftInput
                {
                    ItemTemplateId = source.StageTemplateItems.Single().ItemTemplateId,
                    DueDate = due, ConfirmedDate = confirmed
                }]
            }).ToArray()
        };

        var result = await fixture.SendAsync(command, CancellationToken.None);
        var plan = fixture.Db.TimePlans.Single();
        var stages = plan.TimePlanStages.OrderBy(stage => stage.SortOrder).ToArray();
        source.Name = "Changed source";
        source.StageTemplateItems.Single().ItemTemplate.Name = "Changed source item";

        Assert.True(result.IsSuccess);
        Assert.Equal(2, stages.Length);
        Assert.NotEqual(stages[0].Id, stages[1].Id);
        Assert.Equal(new[] { 1, 2 }, stages.Select(stage => stage.SortOrder));
        Assert.All(stages, stage =>
        {
            Assert.Equal("Snapshot stage", stage.Name);
            var item = Assert.Single(stage.PlanStageItems);
            Assert.Equal("Snapshot item", item.Name);
            Assert.Equal(PlanStageItemStatus.Pending, item.Status);
            Assert.Equal(due, item.DueDate);
            Assert.Equal(confirmed, item.ConfirmedDate);
        });
        Assert.Equal(1, fixture.Db.SaveCount);
    }

    [Fact]
    public async Task Missing_dates_or_inactive_sources_prevent_the_entire_draft_from_being_saved()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var unit = fixture.Unit();
        var source = SeedTemplate(fixture);
        var itemId = source.StageTemplateItems.Single().ItemTemplateId;
        var command = new CreateTimePlanCommand
        {
            ProjectId = unit.ProjectId,
            Input = new TimePlanInput { Name = "Plan", DeliveryDate = fixture.Clock.GetUtcNow() },
            Stages = [
                new StageDraftInput
                {
                    Name = "Valid draft", DeliveryDate = fixture.Clock.GetUtcNow(), Items = []
                },
                new StageDraftInput
                {
                    StageTemplateId = source.Id, DeliveryDate = fixture.Clock.GetUtcNow(),
                    Items = [new StageItemDraftInput { ItemTemplateId = itemId }]
                }
            ]
        };
        var validation = await new CreateTimePlanCommandValidator().ValidateAsync(command);
        var error = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            fixture.SendAsync(command, CancellationToken.None));

        Assert.Contains(validation.Errors, error => error.ErrorMessage == "Pls insert the dates first");
        Assert.Contains(error.Errors, failure => failure.ErrorMessage == "Pls insert the dates first");
        Assert.Empty(fixture.Db.TimePlans);
        Assert.Equal(0, fixture.Db.SaveCount);

        source.IsActive = false;
        var inactive = await fixture.Services.GetRequiredService<
            Brouj.Application.Features.TimePlans.Services.TimePlanDraftBuilder>().BuildAsync(Guid.NewGuid(),
            new StageDraftInput { StageTemplateId = source.Id, DeliveryDate = fixture.Clock.GetUtcNow() }, 1, CancellationToken.None);

        Assert.True(inactive.IsFailure);
    }

    [Fact]
    public async Task Inactive_linked_item_cannot_be_copied_and_existing_project_plan_cannot_be_duplicated()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var unit = fixture.Unit();
        var source = SeedTemplate(fixture);
        source.StageTemplateItems.Single().ItemTemplate.IsActive = false;
        var copy = await fixture.Services.GetRequiredService<
            Brouj.Application.Features.TimePlans.Services.TimePlanDraftBuilder>().BuildAsync(Guid.NewGuid(),
            new StageDraftInput
            {
                StageTemplateId = source.Id,
                DeliveryDate = fixture.Clock.GetUtcNow(),
                Items = [new StageItemDraftInput
                {
                    ItemTemplateId = source.StageTemplateItems.Single().ItemTemplateId, DueDate = fixture.Clock.GetUtcNow()
                }]
            }, 1, CancellationToken.None);
        fixture.Db.TimePlans.Add(new TimePlan { Id = Guid.NewGuid(), ProjectId = unit.ProjectId });
        var duplicate = await fixture.SendAsync(new CreateTimePlanCommand
        {
            ProjectId = unit.ProjectId,
            Input = new TimePlanInput { Name = "Another", DeliveryDate = fixture.Clock.GetUtcNow() }
        }, CancellationToken.None);

        Assert.True(copy.IsFailure && duplicate.IsFailure);
        Assert.Equal(0, fixture.Db.SaveCount);
    }

    [Theory]
    [InlineData(PlanStageItemStatus.Pending, PlanStageItemStatus.Pending)]
    [InlineData(PlanStageItemStatus.Pending, PlanStageItemStatus.Processing)]
    [InlineData(PlanStageItemStatus.Pending, PlanStageItemStatus.Completed)]
    [InlineData(PlanStageItemStatus.Processing, PlanStageItemStatus.Pending)]
    [InlineData(PlanStageItemStatus.Processing, PlanStageItemStatus.Processing)]
    [InlineData(PlanStageItemStatus.Processing, PlanStageItemStatus.Completed)]
    [InlineData(PlanStageItemStatus.Completed, PlanStageItemStatus.Pending)]
    [InlineData(PlanStageItemStatus.Completed, PlanStageItemStatus.Processing)]
    [InlineData(PlanStageItemStatus.Completed, PlanStageItemStatus.Completed)]
    public async Task Admin_can_change_any_item_status_to_any_other_status_without_date_order_checks(
        PlanStageItemStatus before,
        PlanStageItemStatus after)
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var item = new PlanStageItem { Id = Guid.NewGuid(), StageId = Guid.NewGuid(), Status = before };
        fixture.Db.PlanStageItems.Add(item);
        var confirmed = fixture.Clock.GetUtcNow().AddYears(5);
        var command = new ChangePlanStageItemStatusCommand
        {
            Id = item.Id,
            StageId = item.StageId,
            Status = after,
            ConfirmedDate = confirmed
        };
        var validation = await new ChangePlanStageItemStatusCommandValidator().ValidateAsync(command);
        var result = await fixture.SendAsync(command, CancellationToken.None);

        Assert.True(validation.IsValid);
        Assert.True(result.IsSuccess);
        Assert.Equal(after, item.Status);
        Assert.Equal(confirmed, item.ConfirmedDate);
    }

    [Fact]
    public async Task Reordering_requires_the_full_sibling_set_and_uses_contiguous_positions()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var plan = new TimePlan { Id = Guid.NewGuid() };
        fixture.Db.TimePlans.Add(plan);
        var first = new TimePlanStage { Id = Guid.NewGuid(), TimePlanId = plan.Id, SortOrder = 1 };
        var second = new TimePlanStage { Id = Guid.NewGuid(), TimePlanId = plan.Id, SortOrder = 2 };
        fixture.Db.TimePlanStages.AddRange(first, second);

        var wrongParent = await fixture.SendAsync(new ReorderTimePlanStagesCommand
        {
            TimePlanId = plan.Id,
            Items = [new ReorderItemInput { Id = first.Id, SortOrder = 1 }, new ReorderItemInput { Id = Guid.NewGuid(), SortOrder = 2 }]
        }, CancellationToken.None);
        var result = await fixture.SendAsync(new ReorderTimePlanStagesCommand
        {
            TimePlanId = plan.Id,
            Items = [new ReorderItemInput { Id = second.Id, SortOrder = 1 }, new ReorderItemInput { Id = first.Id, SortOrder = 2 }]
        }, CancellationToken.None);

        Assert.True(wrongParent.IsFailure && result.IsSuccess);
        Assert.Equal(2, first.SortOrder);
        Assert.Equal(1, second.SortOrder);
        Assert.Equal(2, fixture.Db.SaveCount);
    }

    [Fact]
    public async Task Deleting_an_ordered_item_compacts_siblings_and_defers_file_removal()
    {
        using var fixture = new FeatureHarness();
        fixture.Staff();
        var stageId = Guid.NewGuid();
        var first = new PlanStageItem { Id = Guid.NewGuid(), StageId = stageId, SortOrder = 1 };
        var second = new PlanStageItem { Id = Guid.NewGuid(), StageId = stageId, SortOrder = 2 };
        fixture.Db.PlanStageItems.AddRange(first, second);
        fixture.Db.StageItemImages.Add(new StageItemImage
        {
            Id = Guid.NewGuid(),
            ItemId = first.Id,
            Item = first,
            ImagePath = "generated.png",
            SortOrder = 1
        });

        var result = await fixture.SendAsync(
            new DeletePlanStageItemCommand { Id = first.Id, StageId = stageId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, second.SortOrder);
        // The registered committed-effects behavior removes files after the transaction completes.
        Assert.Equal("generated.png", Assert.Single(fixture.Files.Deleted));
    }

    [Fact]
    public async Task Time_plan_read_is_available_to_an_active_customer_without_a_reservation()
    {
        using var fixture = new FeatureHarness();
        fixture.Customer();
        var plan = new TimePlan { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid(), Name = "Customer plan" };
        fixture.Db.TimePlans.Add(plan);

        var result = await fixture.SendAsync(new GetProjectTimePlanQuery { ProjectId = plan.ProjectId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(plan.Id, result.Value.Id);
    }

    private static StageTemplate SeedTemplate(FeatureHarness fixture)
    {
        var template = new StageTemplate { Id = Guid.NewGuid(), Name = "Snapshot stage", Code = "stage", IsActive = true };
        var item = new ItemTemplate { Id = Guid.NewGuid(), Name = "Snapshot item", Code = "item", IsActive = true };
        var mapping = new StageTemplateItem
        {
            Id = Guid.NewGuid(),
            StageTemplateId = template.Id,
            StageTemplate = template,
            ItemTemplateId = item.Id,
            ItemTemplate = item,
            SortOrder = 1
        };
        template.StageTemplateItems.Add(mapping);
        fixture.Db.StageTemplates.Add(template);
        fixture.Db.ItemTemplates.Add(item);
        fixture.Db.StageTemplateItems.Add(mapping);

        return template;
    }
}
