using Brouj.Domain.Enums;
using Brouj.Domain.Extensions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Brouj.Infrastructure.Persistence.Converters;

public sealed class TimePlanStageStatusConvertor() : ValueConverter<TimePlanStageStatus, string>(
    value => value.ToDatabaseString(),
    value => TimePlanStageExtensions.ToOrderStatus(value));
