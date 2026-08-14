using AMS.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Identity.Persistence.Configurations;

/// <summary>Mirrors <c>[Identity].[Role]</c> in AMS_Consolidated_Design_v2.sql.</summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Role");

        builder.HasKey(x => x.Id).HasName("PK_Role");

        builder.Property(x => x.RoleName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.Property(x => x.IsSystemRole).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => x.RoleName)
            .IsUnique()
            .HasDatabaseName("UX_Role_Name");
    }
}
