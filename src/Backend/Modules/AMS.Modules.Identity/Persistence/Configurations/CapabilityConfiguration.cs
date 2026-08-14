using AMS.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Identity.Persistence.Configurations;

/// <summary>Mirrors <c>[Identity].[Capability]</c> in AMS_Consolidated_Design_v2.sql.</summary>
public sealed class CapabilityConfiguration : IEntityTypeConfiguration<Capability>
{
    public void Configure(EntityTypeBuilder<Capability> builder)
    {
        builder.ToTable("Capability");

        builder.HasKey(x => x.Name).HasName("PK_Capability");

        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300);

        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
    }
}
