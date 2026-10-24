using Brouj.Domain.Enums;
using Brouj.Domain.Extensions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Brouj.Infrastructure.Persistence.Converters;

public sealed class ProjectTypeConverter() : ValueConverter<ProjectType, string>(
    value => value.ToDatabaseString(),
    value => value.ToProjectType());
