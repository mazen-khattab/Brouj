using Brouj.Domain.Enums;
using Brouj.Domain.Extensions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Brouj.Infrastructure.Persistence.Converters;

public sealed class ActivityActionConverter() : ValueConverter<ActivityAction, string>(
    value => value.ToDatabaseString(),
    value => value.ToActivityAction());
