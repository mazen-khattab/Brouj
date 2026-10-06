using Brouj.Domain.Enums;
using Brouj.Domain.Extensions;

namespace Brouj.Domain.UnitTests.Extensions;

public sealed class EnumExtensionsTests
{
    [Theory]
    [InlineData(ProjectType.Regular, "regular")]
    [InlineData(ProjectType.Initiative, "initiative")]
    public void ProjectTypeMapping_RoundTrips(ProjectType value, string databaseValue)
    {
        Assert.Equal(databaseValue, value.ToDatabaseString());
        Assert.Equal(value, databaseValue.ToProjectType());
    }

    [Theory]
    [InlineData(ReservationStatus.Processing, "processing")]
    [InlineData(ReservationStatus.Cancelled, "cancelled")]
    [InlineData(ReservationStatus.Confirmed, "confirmed")]
    public void ReservationStatusMapping_RoundTrips(
        ReservationStatus value,
        string databaseValue)
    {
        Assert.Equal(databaseValue, value.ToDatabaseString());
        Assert.Equal(value, databaseValue.ToReservationStatus());
    }

    [Theory]
    [InlineData(OrderStatus.Pending, "pending")]
    [InlineData(OrderStatus.Processing, "processing")]
    [InlineData(OrderStatus.Completed, "completed")]
    [InlineData(OrderStatus.Cancelled, "cancelled")]
    public void OrderStatusMapping_RoundTrips(OrderStatus value, string databaseValue)
    {
        Assert.Equal(databaseValue, value.ToDatabaseString());
        Assert.Equal(value, databaseValue.ToOrderStatus());
    }

    [Theory]
    [InlineData(PlanStageItemStatus.Pending, "pending")]
    [InlineData(PlanStageItemStatus.Processing, "processing")]
    [InlineData(PlanStageItemStatus.Completed, "completed")]
    public void PlanStageItemStatusMapping_RoundTrips(
        PlanStageItemStatus value,
        string databaseValue)
    {
        Assert.Equal(databaseValue, value.ToDatabaseString());
        Assert.Equal(value, databaseValue.ToPlanStageItemStatus());
    }

    [Theory]
    [InlineData(UserRole.SuperAdmin, "super admin")]
    [InlineData(UserRole.Admin, "admin")]
    public void UserRoleMapping_RoundTrips(UserRole value, string databaseValue)
    {
        Assert.Equal(databaseValue, value.ToDatabaseString());
        Assert.Equal(value, databaseValue.ToUserRole());
    }

    [Theory]
    [InlineData(ActivityAction.Created, "created")]
    [InlineData(ActivityAction.Updated, "updated")]
    [InlineData(ActivityAction.Deleted, "deleted")]
    public void ActivityActionMapping_RoundTrips(ActivityAction value, string databaseValue)
    {
        Assert.Equal(databaseValue, value.ToDatabaseString());
        Assert.Equal(value, databaseValue.ToActivityAction());
    }

    [Fact]
    public void ProjectTypeMapping_RejectsUnknownDatabaseValue()
    {
        Assert.Throws<ArgumentException>(() => "unknown".ToProjectType());
    }

    [Fact]
    public void ReservationStatusMapping_RejectsUnknownDatabaseValue()
    {
        Assert.Throws<ArgumentException>(() => "unknown".ToReservationStatus());
    }

    [Fact]
    public void OrderStatusMapping_RejectsUnknownDatabaseValue()
    {
        Assert.Throws<ArgumentException>(() => "unknown".ToOrderStatus());
    }

    [Fact]
    public void PlanStageItemStatusMapping_RejectsUnknownDatabaseValue()
    {
        Assert.Throws<ArgumentException>(() => "unknown".ToPlanStageItemStatus());
    }

    [Fact]
    public void UserRoleMapping_RejectsUnknownDatabaseValue()
    {
        Assert.Throws<ArgumentException>(() => "unknown".ToUserRole());
    }

    [Fact]
    public void ActivityActionMapping_RejectsUnknownDatabaseValue()
    {
        Assert.Throws<ArgumentException>(() => "unknown".ToActivityAction());
    }

    [Fact]
    public void EnumMappings_RejectUndefinedEnumValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ProjectType)int.MaxValue).ToDatabaseString());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ReservationStatus)int.MaxValue).ToDatabaseString());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((OrderStatus)int.MaxValue).ToDatabaseString());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((PlanStageItemStatus)int.MaxValue).ToDatabaseString());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((UserRole)int.MaxValue).ToDatabaseString());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ActivityAction)int.MaxValue).ToDatabaseString());
    }
}
