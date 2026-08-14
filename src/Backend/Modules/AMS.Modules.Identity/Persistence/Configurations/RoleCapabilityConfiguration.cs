using AMS.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Identity.Persistence.Configurations;

/// <summary>Mirrors <c>[Identity].[RoleCapability]</c> in AMS_Consolidated_Design_v2.sql.</summary>
public sealed class RoleCapabilityConfiguration : IEntityTypeConfiguration<RoleCapability>
{
    public void Configure(EntityTypeBuilder<RoleCapability> builder)
    {
        builder.ToTable("RoleCapability");

        builder.HasKey(x => new { x.RoleId, x.CapabilityName }).HasName("PK_RoleCapability");

        builder.Property(x => x.CapabilityName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.GrantedOnUtc).IsRequired();
        builder.Property(x => x.GrantedBy).HasMaxLength(100);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RoleCapability_Role_RoleId");

        // R2-6. Deleting a capability removes its grants: retiring a capability
        // is a code-level decision and the grants are meaningless without it.
        builder.HasOne<Capability>()
            .WithMany()
            .HasForeignKey(x => x.CapabilityName)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RoleCapability_Capability_CapabilityName");

        builder.HasIndex(x => x.CapabilityName).HasDatabaseName("IX_RoleCapability_CapabilityName");
    }
}
