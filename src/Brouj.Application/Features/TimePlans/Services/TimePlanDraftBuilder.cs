using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.DTOs;
using Brouj.Application.Features.TimePlans.Errors;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Services;

public sealed class TimePlanDraftBuilder
{
    private readonly IApplicationDbContext _db;

    public TimePlanDraftBuilder(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TimePlanStage>> BuildAsync(
        Guid timePlanId,
        StageDraftInput input,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        if (input.DeliveryDate is null || input.Items.Any(item => item.DueDate is null))
        {
            return Result<TimePlanStage>.Failure(TimePlanErrors.Validation("Pls insert the dates first"));
        }

        var name = input.Name.Trim();
        var items = new List<(string Name, StageItemDraftInput Input)>();

        if (input.StageTemplateId is Guid templateId)
        {
            var template = await _db.StageTemplates.AsNoTracking()
                .SingleOrDefaultAsync(template => template.Id == templateId, cancellationToken);

            if (template is null || !template.IsActive)
            {
                return Result<TimePlanStage>.Failure(TimePlanErrors.Conflict("An active stage template is required."));
            }

            var mappings = await _db.StageTemplateItems.AsNoTracking()
                .Where(mapping => mapping.StageTemplateId == templateId)
                .OrderBy(mapping => mapping.SortOrder)
                .Select(mapping => new
                {
                    mapping.ItemTemplateId,
                    mapping.ItemTemplate.Name,
                    mapping.ItemTemplate.IsActive
                }).ToListAsync(cancellationToken);

            var suppliedIds = input.Items.Select(item => item.ItemTemplateId).ToArray();

            if (mappings.Any(mapping => !mapping.IsActive) || suppliedIds.Any(id => id is null) ||
                suppliedIds.Distinct().Count() != mappings.Count || suppliedIds.Length != mappings.Count ||
                mappings.Any(mapping => !suppliedIds.Contains(mapping.ItemTemplateId)))
            {
                return Result<TimePlanStage>.Failure(
                    TimePlanErrors.Conflict("Supply dates for every active item in the selected template."));
            }

            name = template.Name;

            foreach (var mapping in mappings)
            {
                items.Add((mapping.Name, input.Items.Single(item => item.ItemTemplateId == mapping.ItemTemplateId)));
            }
        }
        else
        {
            if (input.Items.Any(item => item.ItemTemplateId is not null))
            {
                return Result<TimePlanStage>.Failure(
                    TimePlanErrors.Validation("Item template references require a stage template."));
            }

            items.AddRange(input.Items.Select(item => (item.Name.Trim(), item)));
        }

        // Construct a detached snapshot only after every source and manual date has passed validation.
        var stage = new TimePlanStage
        {
            Id = Guid.NewGuid(),
            TimePlanId = timePlanId,
            Name = name,
            DeliveryDate = input.DeliveryDate.Value,
            SortOrder = sortOrder
        };

        for (var index = 0; index < items.Count; index++)
        {
            var source = items[index];
            stage.PlanStageItems.Add(new PlanStageItem
            {
                Id = Guid.NewGuid(),
                StageId = stage.Id,
                Name = source.Name,
                Status = PlanStageItemStatus.Pending,
                SortOrder = index + 1,
                DueDate = source.Input.DueDate!.Value,
                ConfirmedDate = source.Input.ConfirmedDate
            });
        }

        return Result<TimePlanStage>.Success(stage);
    }
}
