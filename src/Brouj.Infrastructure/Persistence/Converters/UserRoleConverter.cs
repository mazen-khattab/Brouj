using Brouj.Domain.Enums;
using Brouj.Domain.Extensions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Brouj.Infrastructure.Persistence.Converters;

public sealed class UserRoleConverter() : ValueConverter<UserRole, string>(
    value => value.ToDatabaseString(),
    value => value.ToUserRole());
