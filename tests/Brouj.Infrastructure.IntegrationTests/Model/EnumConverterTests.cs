using Brouj.Domain.Enums;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Brouj.Infrastructure.IntegrationTests.Model;

public sealed class EnumConverterTests
{
    public static IEnumerable<object[]> Members()
    {
        yield return [typeof(ProjectType), "Regular", "regular"];
        yield return [typeof(ProjectType), "Initiative", "initiative"];
        yield return [typeof(ReservationStatus), "Processing", "processing"];
        yield return [typeof(ReservationStatus), "Cancelled", "cancelled"];
        yield return [typeof(ReservationStatus), "Confirmed", "confirmed"];
        yield return [typeof(OrderStatus), "Pending", "pending"];
        yield return [typeof(OrderStatus), "Processing", "processing"];
        yield return [typeof(OrderStatus), "Completed", "completed"];
        yield return [typeof(OrderStatus), "Cancelled", "cancelled"];
        yield return [typeof(PlanStageItemStatus), "Pending", "pending"];
        yield return [typeof(PlanStageItemStatus), "Processing", "processing"];
        yield return [typeof(PlanStageItemStatus), "Completed", "completed"];
        yield return [typeof(UserRole), "SuperAdmin", "super admin"];
        yield return [typeof(UserRole), "Admin", "admin"];
        yield return [typeof(ActivityAction), "Created", "created"];
        yield return [typeof(ActivityAction), "Updated", "updated"];
        yield return [typeof(ActivityAction), "Deleted", "deleted"];
    }

    public static IEnumerable<object[]> Types() => Members().Select(row => (Type)row[0]).Distinct().Select(type => new object[] { type });

    [Theory]
    [MemberData(nameof(Members))]
    public void Every_approved_member_round_trips_to_its_exact_DB_string(Type type, string member, string persisted)
    {
        using var context = TestContext.Create();
        var converter = Converter(context, type);
        var value = Enum.Parse(type, member);
        Assert.Equal(persisted, converter.ConvertToProvider(value));
        Assert.Equal(value, converter.ConvertFromProvider(persisted));
    }

    [Theory]
    [MemberData(nameof(Types))]
    public void Unknown_enum_members_and_DB_strings_fail_explicitly(Type type)
    {
        using var context = TestContext.Create();
        var converter = Converter(context, type);
        Assert.Throws<ArgumentOutOfRangeException>(() => converter.ConvertToProvider(Enum.ToObject(type, -1)));
        Assert.Throws<ArgumentException>(() => converter.ConvertFromProvider("unknown"));
        Assert.Throws<ArgumentException>(() => converter.ConvertFromProvider(""));
        var persisted = (string)Members().First(row => (Type)row[0] == type)[2];
        Assert.Throws<ArgumentException>(() => converter.ConvertFromProvider(persisted.ToUpperInvariant()));
    }

    private static ValueConverter Converter(Brouj.Infrastructure.Persistence.ApplicationDbContext context, Type type) =>
        Assert.Single(context.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()),
            property => property.ClrType == type).GetTypeMapping().Converter!;
}
