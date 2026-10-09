using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.Persistence;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

internal sealed record TestGraph(
    City City, Area Area, BaseProject Project, BaseUnit Unit, BaseUnit OtherUnit,
    Customer Customer, User User, TimePlan Plan, PlanStageItem Item,
    StageTemplate StageTemplate, ItemTemplate ItemTemplate, Reservation Reservation,
    Reservation OtherReservation, Order Order, ActivityLog Log)
{
    public static async Task<TestGraph> SeedAsync(
        ApplicationDbContext context,
        bool reservations = true,
        bool order = true,
        bool log = false)
    {
        var now = DateTimeOffset.UtcNow;
        var suffix = Guid.NewGuid().ToString("N");
        var city = new City { Name = "city-" + suffix };
        var area = new Area { Name = "area", City = city };
        var project = new BaseProject { Name = "project", Area = area, Price = 100, Size = 500, ProjectType = ProjectType.Regular };
        var unit = new BaseUnit { Project = project, Number = "U1", MeterPrice = 10, Size = 50 };
        var otherUnit = new BaseUnit { Project = project, Number = "U2", MeterPrice = 10, Size = 60 };
        var customer = new Customer
        {
            FName = "Test", LName = "Customer", Phone = suffix, IdentityNumber = suffix,
            Email = suffix + "@example.test", HashPassword = "synthetic-hash"
        };
        var user = new User { Name = "staff", Email = suffix + "@staff.test", Phone = suffix, HashPassword = "synthetic-hash", Role = UserRole.Admin };
        var plan = new TimePlan { Project = project, Name = "plan", DeliveryDate = now.AddMonths(1) };
        var stage = new TimePlanStage { TimePlan = plan, Name = "stage", SortOrder = 1, DeliveryDate = now.AddDays(10) };
        var item = new PlanStageItem { Stage = stage, Name = "item", SortOrder = 1, DueDate = now.AddDays(1), Status = PlanStageItemStatus.Pending };
        var stageTemplate = new StageTemplate { Name = "stage template", Code = "stage", IsActive = true };
        var itemTemplate = new ItemTemplate { Name = "item template", Code = "item", IsActive = true };
        var reservation = new Reservation { Unit = unit, Customer = customer, Number = "R1", ReservationDate = now, Status = ReservationStatus.Processing };
        var otherReservation = new Reservation { Unit = otherUnit, Customer = customer, Number = "R2", ReservationDate = now, Status = ReservationStatus.Confirmed };
        var purchase = new Order { Reservation = reservation, Number = "O1", TotalPrice = 500, Status = OrderStatus.Pending };

        context.AddRange(city, area, project, unit, otherUnit, customer, user, plan, stage, item, stageTemplate, itemTemplate);
        context.AddRange(
            new AreaAmenity { Area = area, Name = "area amenity", Value = "yes" },
            new InitiativeProject { Project = project, NeighborPrice = 100, DrawDate = now },
            new ProjectAmenity { Project = project, Name = "project amenity", Value = "yes" },
            new ProjectImage { Project = project, ImagePath = "project.jpg", AltText = null },
            new InitiativeUnit { Unit = unit, NeighborMeterPrice = 10, Percentage = 10 },
            new UnitAmenity { Unit = unit, Name = "unit amenity", Value = "yes" },
            new UnitImage { Unit = unit, ImagePath = "unit.jpg", AltText = null },
            new StageItemImage { Item = item, ImagePath = "item.jpg", SortOrder = 1 },
            new StageTemplateItem { StageTemplate = stageTemplate, ItemTemplate = itemTemplate, SortOrder = 1 },
            new CustomerRefreshToken { Customer = customer, Token = "synthetic-customer-token", IsActive = true, ExpDate = now.AddDays(1) },
            new UserRefreshToken { User = user, Token = "synthetic-user-token", IsActive = true, ExpDate = now.AddDays(1) },
            new TermsAndConditions { Title = "terms", Content = "test terms", VersionNumber = 1, IsActive = true, EffectiveDate = now });

        if (reservations)
        {
            context.AddRange(reservation, otherReservation);
            if (order)
            {
                context.Orders.Add(purchase);
            }
        }

        var activity = new ActivityLog { User = user, EntityType = nameof(City), EntityId = city.Id, Action = ActivityAction.Created, NewData = "{}" };
        if (log)
        {
            context.ActivityLogs.Add(activity);
        }

        await context.SaveChangesAsync();
        return new TestGraph(city, area, project, unit, otherUnit, customer, user, plan, item, stageTemplate,
            itemTemplate, reservation, otherReservation, purchase, activity);
    }

    public Reservation NewReservation(string number, ReservationStatus status = ReservationStatus.Processing) =>
        new() { CustomerId = Customer.Id, UnitId = Unit.Id, Number = number, Status = status, ReservationDate = DateTimeOffset.UtcNow };
}
