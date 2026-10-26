using Brouj.Application.Abstractions.Persistence;
using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Brouj.API.IntegrationTests.Support;

/// <summary>Exercises handler decisions only. It does not emulate tracking, SQL constraints, cascades or rollback.</summary>
internal sealed class ApiTestDb : IApplicationDbContext
{
    public DbSet<ActivityLog> ActivityLogs { get; } = new ListDbSet<ActivityLog>();

    public DbSet<Area> Areas { get; } = new ListDbSet<Area>();

    public DbSet<AreaAmenity> AreaAmenities { get; } = new ListDbSet<AreaAmenity>();

    public DbSet<BaseProject> BaseProjects { get; } = new ListDbSet<BaseProject>();

    public DbSet<BaseUnit> BaseUnits { get; } = new ListDbSet<BaseUnit>();

    public DbSet<City> Cities { get; } = new ListDbSet<City>();

    public DbSet<Customer> Customers { get; }

    public DbSet<CustomerRefreshToken> CustomerRefreshTokens { get; } = new ListDbSet<CustomerRefreshToken>();

    public DbSet<InitiativeProject> InitiativeProjects { get; } = new ListDbSet<InitiativeProject>();

    public DbSet<InitiativeUnit> InitiativeUnits { get; } = new ListDbSet<InitiativeUnit>();

    public DbSet<ItemTemplate> ItemTemplates { get; } = new ListDbSet<ItemTemplate>();

    public DbSet<Order> Orders { get; } = new ListDbSet<Order>();

    public DbSet<PlanStageItem> PlanStageItems { get; } = new ListDbSet<PlanStageItem>();

    public DbSet<ProjectAmenity> ProjectAmenities { get; } = new ListDbSet<ProjectAmenity>();

    public DbSet<ProjectImage> ProjectImages { get; } = new ListDbSet<ProjectImage>();

    public DbSet<Reservation> Reservations { get; } = new ListDbSet<Reservation>();

    public DbSet<StageItemImage> StageItemImages { get; } = new ListDbSet<StageItemImage>();

    public DbSet<StageTemplate> StageTemplates { get; } = new ListDbSet<StageTemplate>();

    public DbSet<StageTemplateItem> StageTemplateItems { get; } = new ListDbSet<StageTemplateItem>();

    public DbSet<TermsAndConditions> TermsAndConditions { get; } = new ListDbSet<TermsAndConditions>();

    public DbSet<TimePlan> TimePlans { get; } = new ListDbSet<TimePlan>();

    public DbSet<TimePlanStage> TimePlanStages { get; } = new ListDbSet<TimePlanStage>();

    public DbSet<UnitAmenity> UnitAmenities { get; } = new ListDbSet<UnitAmenity>();

    public DbSet<UnitImage> UnitImages { get; } = new ListDbSet<UnitImage>();

    public DbSet<User> Users { get; }

    public DbSet<UserRefreshToken> UserRefreshTokens { get; } = new ListDbSet<UserRefreshToken>();

    public int SaveCount { get; private set; }

    public int AccountReadCount { get; private set; }

    public ApiTestDb()
    {
        Customers = new ListDbSet<Customer>(() => AccountReadCount++);
        Users = new ListDbSet<User>(() => AccountReadCount++);
    }

    public bool FailSave { get; set; }

    public Exception? SaveException { get; set; }

    public CancellationToken LastCancellationToken { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastCancellationToken = cancellationToken;
        SaveCount++;

        if (SaveException is not null)
        {
            throw SaveException;
        }

        if (FailSave)
        {
            throw new Microsoft.EntityFrameworkCore.DbUpdateException("synthetic private provider diagnostic");
        }

        return Task.FromResult(1);
    }
}
