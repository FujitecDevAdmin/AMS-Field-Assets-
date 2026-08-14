using AMS.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Identity.Persistence.Configurations;

/// <summary>Mirrors <c>[Identity].[UserCapabilityOverride]</c> in AMS_Consolidated_Design_v2.sql.</summary>
public sealed class UserCapabilityOverrideConfiguration : IEntityTypeConfiguration<UserCapabilityOverride>
{
    public void Configure(EntityTypeBuilder<UserCapabilityOverride> builder)
    {
        builder.ToTable("UserCapabilityOverride");

        builder.HasKey(x => new { x.UserId, x.CapabilityName }).HasName("PK_UserCapabilityOverride");

        builder.Property(x => x.CapabilityName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.IsGranted).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(300);
        builder.Property(x => x.GrantedOnUtc).IsRequired();
        builder.Property(x => x.GrantedBy).HasMaxLength(100);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserCapabilityOverride_User_UserId");

        builder.HasOne<Capability>()
            .WithMany()
            .HasForeignKey(x => x.CapabilityName)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserCapabilityOverride_Capability_CapabilityName");

        builder.HasIndex(x => x.CapabilityName).HasDatabaseName("IX_UserCapabilityOverride_CapabilityName");
    }
}
