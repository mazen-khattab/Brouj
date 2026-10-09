using Brouj.Domain.Enums;
using Brouj.Domain.Extensions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Brouj.Infrastructure.Persistence.Converters;

public sealed class PlanStageItemStatusConverter() : ValueConverter<PlanStageItemStatus, string>(
    value => value.ToDatabaseString(),
    value => value.ToPlanStageItemStatus());
