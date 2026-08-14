using AMS.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace AMS.Modules.Identity.Persistence;

/// <summary>
/// The Identity module's context. Owns schema <c>[Identity]</c> and nothing
/// else (docs/01 §2 rule 1).
/// </summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    /// <summary>The schema this module owns, and the migrations-history schema.</summary>
    public const string SchemaName = "Identity";

    public DbSet<Capability> Capabilities => Set<Capability>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RoleCapability> RoleCapabilities => Set<RoleCapability>();

    public DbSet<UserCapabilityOverride> UserCapabilityOverrides => Set<UserCapabilityOverride>();

    public DbSet<UserBranch> UserBranches => Set<UserBranch>();

    public DbSet<UserRecoveryCode> UserRecoveryCodes => Set<UserRecoveryCode>();

    /// <summary>
    /// Drops EF's automatic index-per-foreign-key convention.
    /// </summary>
    /// <remarks>
    /// The reviewed design decides its own indexes: it adds one where a query
    /// needs it and leaves it out where nothing reads that way. All sixteen
    /// contexts do this, so the model is the script and nothing else.
    /// </remarks>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Conventions.Remove<ForeignKeyIndexConvention>();
    }

    // The parameter must be named modelBuilder: CA1725 requires an override to
    // keep the base class's parameter names, and warnings are errors here.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        // This assembly only. A configuration from another module appearing in
        // this model would put another schema's table under this context.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
