using AMS.Modules.Contracts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace AMS.Modules.Contracts.Persistence;

/// <summary>
/// The Contracts module's context. Owns schema <c>[Contracts]</c> and nothing
/// else (docs/01 §2 rule 1).
/// </summary>
public sealed class ContractsDbContext(DbContextOptions<ContractsDbContext> options) : DbContext(options)
{
    /// <summary>The schema this module owns, and its migrations-history schema.</summary>
    public const string SchemaName = "Contracts";

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<ContractAsset> ContractAssets => Set<ContractAsset>();

    public DbSet<ContractDocument> ContractDocuments => Set<ContractDocument>();

    public DbSet<ContractReminderLog> ContractReminderLogs => Set<ContractReminderLog>();

    public DbSet<ContractReminderSetting> ContractReminderSettings => Set<ContractReminderSetting>();

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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContractsDbContext).Assembly);
    }
}
