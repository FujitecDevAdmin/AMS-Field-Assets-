using AMS.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Identity.Persistence.Configurations;

/// <summary>Mirrors <c>[Identity].[UserRecoveryCode]</c> in AMS_Consolidated_Design_v2.sql.</summary>
public sealed class UserRecoveryCodeConfiguration : IEntityTypeConfiguration<UserRecoveryCode>
{
    public void Configure(EntityTypeBuilder<UserRecoveryCode> builder)
    {
        builder.ToTable("UserRecoveryCode");

        builder.HasKey(x => x.Id).HasName("PK_UserRecoveryCode");

        builder.Property(x => x.CodeHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserRecoveryCode_User_UserId");

        // R2-15: filtered. The only question ever asked of this table is
        // "which of this user's codes are still unused", and a used code is
        // dead weight in the index.
        builder.HasIndex(x => x.UserId)
            .HasFilter("[UsedOnUtc] IS NULL")
            .HasDatabaseName("IX_UserRecoveryCode_UserUnused");
    }
}
