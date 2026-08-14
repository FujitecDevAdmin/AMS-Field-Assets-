using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace AMS.Modules.Assets.Persistence;

/// <summary>
/// The Assets module's context. Owns schema <c>[Assets]</c> and nothing
/// else (docs/01 §2 rule 1).
/// </summary>
public sealed class AssetsDbContext(DbContextOptions<AssetsDbContext> options) : DbContext(options)
{
    /// <summary>The schema this module owns, and its migrations-history schema.</summary>
    public const string SchemaName = "Assets";

    public DbSet<Asset> Assets => Set<Asset>();

    public DbSet<AssetClass> AssetClasses => Set<AssetClass>();

    public DbSet<AssetCustomValue> AssetCustomValues => Set<AssetCustomValue>();

    public DbSet<AssetDepreciationEntry> AssetDepreciationEntries => Set<AssetDepreciationEntry>();

    public DbSet<AssetDisposal> AssetDisposals => Set<AssetDisposal>();

    public DbSet<AssetEvent> AssetEvents => Set<AssetEvent>();

    public DbSet<AssetFinance> AssetFinances => Set<AssetFinance>();

    public DbSet<AssetHardwareDetail> AssetHardwareDetails => Set<AssetHardwareDetail>();

    public DbSet<AssetHolding> AssetHoldings => Set<AssetHolding>();

    public DbSet<AssetInstrumentDetail> AssetInstrumentDetails => Set<AssetInstrumentDetail>();

    public DbSet<AssetPurchaseDetail> AssetPurchaseDetails => Set<AssetPurchaseDetail>();

    public DbSet<AssetSoftwareDetail> AssetSoftwareDetails => Set<AssetSoftwareDetail>();

    public DbSet<AssetStatus> AssetStatuses => Set<AssetStatus>();

    public DbSet<AssetType> AssetTypes => Set<AssetType>();

    public DbSet<AssetVehicleDetail> AssetVehicleDetails => Set<AssetVehicleDetail>();

    public DbSet<ChartOfAccount> ChartOfAccounts => Set<ChartOfAccount>();

    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();

    public DbSet<CustomFieldOption> CustomFieldOptions => Set<CustomFieldOption>();

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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetsDbContext).Assembly);
    }
}
