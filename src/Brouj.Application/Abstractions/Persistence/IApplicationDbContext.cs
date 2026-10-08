using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<ActivityLog> ActivityLogs { get; }
    DbSet<Area> Areas { get; }
    DbSet<AreaAmenity> AreaAmenities { get; }
    DbSet<BaseProject> BaseProjects { get; }
    DbSet<BaseUnit> BaseUnits { get; }
    DbSet<City> Cities { get; }
    DbSet<Customer> Customers { get; }
    DbSet<CustomerRefreshToken> CustomerRefreshTokens { get; }
    DbSet<InitiativeProject> InitiativeProjects { get; }
    DbSet<InitiativeUnit> InitiativeUnits { get; }
    DbSet<ItemTemplate> ItemTemplates { get; }
    DbSet<Order> Orders { get; }
    DbSet<PlanStageItem> PlanStageItems { get; }
    DbSet<ProjectAmenity> ProjectAmenities { get; }
    DbSet<ProjectImage> ProjectImages { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<StageItemImage> StageItemImages { get; }
    DbSet<StageTemplate> StageTemplates { get; }
    DbSet<StageTemplateItem> StageTemplateItems { get; }
    DbSet<TermsAndConditions> TermsAndConditions { get; }
    DbSet<TimePlan> TimePlans { get; }
    DbSet<TimePlanStage> TimePlanStages { get; }
    DbSet<UnitAmenity> UnitAmenities { get; }
    DbSet<UnitImage> UnitImages { get; }
    DbSet<User> Users { get; }
    DbSet<UserRefreshToken> UserRefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
