using AMS.Modules.DataImport.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace AMS.Modules.DataImport.Persistence;

/// <summary>
/// The DataImport module's context. Owns schema <c>[DataImport]</c> and nothing
/// else (docs/01 §2 rule 1).
/// </summary>
public sealed class DataImportDbContext(DbContextOptions<DataImportDbContext> options) : DbContext(options)
{
    /// <summary>The schema this module owns, and its migrations-history schema.</summary>
    public const string SchemaName = "DataImport";

    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();

    public DbSet<ImportError> ImportErrors => Set<ImportError>();

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

        modelBuilder.HasSequence<long>("ImportBatchNumberSequence", SchemaName)
            .StartsAt(1)
            .IncrementsBy(1);

        // This assembly only. A configuration from another module would put
        // another schema's table under this context.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataImportDbContext).Assembly);
    }
}
