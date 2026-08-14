using AMS.Modules.ServiceLevel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace AMS.Modules.ServiceLevel.Persistence;

/// <summary>
/// The ServiceLevel module's context. Owns schema <c>[ServiceLevel]</c> and nothing
/// else (docs/01 §2 rule 1).
/// </summary>
public sealed class ServiceLevelDbContext(DbContextOptions<ServiceLevelDbContext> options) : DbContext(options)
{
    /// <summary>The schema this module owns, and its migrations-history schema.</summary>
    public const string SchemaName = "ServiceLevel";

    public DbSet<HolidayCalendar> HolidayCalendars => Set<HolidayCalendar>();

    public DbSet<HolidayLocation> HolidayLocations => Set<HolidayLocation>();

    public DbSet<LocationOperationalDay> LocationOperationalDays => Set<LocationOperationalDay>();

    public DbSet<LocationOperationalHour> LocationOperationalHours => Set<LocationOperationalHour>();

    public DbSet<LocationSaturdayRule> LocationSaturdayRules => Set<LocationSaturdayRule>();

    public DbSet<SlaEscalation> SlaEscalations => Set<SlaEscalation>();

    public DbSet<SlaEscalationLog> SlaEscalationLogs => Set<SlaEscalationLog>();

    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();

    /// <summary>
    /// Drops EF's automatic index-per-foreign-key convention.
    /// </summary>
    /// <remarks>
    /// The reviewed design decides its own indexes: it adds one where a
    /// query needs it (IX_UserRole_RoleId, IX_RoleCapability_CapabilityName)
    /// and leaves it out where nothing reads that way. Letting EF add one
    /// per foreign key produced 14 indexes the script never asked for -
    /// each of them a write cost on a table somebody measured.
    /// </remarks>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Conventions.Remove<ForeignKeyIndexConvention>();
    }

    // The parameter must be named modelBuilder: CA1725 requires an override
    // to keep the base member's parameter names, and warnings are errors.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        // This assembly only. A configuration from another module would put
        // another schema's table under this context.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServiceLevelDbContext).Assembly);
    }
}
