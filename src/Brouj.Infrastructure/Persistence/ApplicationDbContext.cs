using Brouj.Application.Abstractions.Persistence;
using Brouj.Domain.Entities;
using Brouj.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly AuditableEntityInterceptor _auditableInterceptor;
    private readonly ActivityLogInterceptor _activityLogInterceptor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditableEntityInterceptor auditableInterceptor,
        ActivityLogInterceptor activityLogInterceptor) : base(options)
    {
        _auditableInterceptor = auditableInterceptor;
        _activityLogInterceptor = activityLogInterceptor;
        // Audit rows and their changes must commit together, even for a single SQL batch.
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
    }

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<AreaAmenity> AreaAmenities => Set<AreaAmenity>();
    public DbSet<BaseProject> BaseProjects => Set<BaseProject>();
    public DbSet<BaseUnit> BaseUnits => Set<BaseUnit>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerRefreshToken> CustomerRefreshTokens => Set<CustomerRefreshToken>();
    public DbSet<InitiativeProject> InitiativeProjects => Set<InitiativeProject>();
    public DbSet<InitiativeUnit> InitiativeUnits => Set<InitiativeUnit>();
    public DbSet<ItemTemplate> ItemTemplates => Set<ItemTemplate>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<PlanStageItem> PlanStageItems => Set<PlanStageItem>();
    public DbSet<ProjectAmenity> ProjectAmenities => Set<ProjectAmenity>();
    public DbSet<ProjectImage> ProjectImages => Set<ProjectImage>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<StageItemImage> StageItemImages => Set<StageItemImage>();
    public DbSet<StageTemplate> StageTemplates => Set<StageTemplate>();
    public DbSet<StageTemplateItem> StageTemplateItems => Set<StageTemplateItem>();
    public DbSet<TermsAndConditions> TermsAndConditions => Set<TermsAndConditions>();
    public DbSet<TimePlan> TimePlans => Set<TimePlan>();
    public DbSet<TimePlanStage> TimePlanStages => Set<TimePlanStage>();
    public DbSet<UnitAmenity> UnitAmenities => Set<UnitAmenity>();
    public DbSet<UnitImage> UnitImages => Set<UnitImage>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditableInterceptor, _activityLogInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new NotSupportedException("Persistence uses asynchronous I/O. Use SaveChangesAsync.");

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(true, cancellationToken);

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch
        {
            // Also covers failures in SavingChanges callbacks before EF emits SaveChangesFailed.
            _activityLogInterceptor.DiscardPendingLogs(this);
            throw;
        }
    }
}
