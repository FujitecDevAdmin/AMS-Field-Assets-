using AMS.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Identity.Persistence.Configurations;

/// <summary>Mirrors <c>[Identity].[UserBranch]</c> in AMS_Consolidated_Design_v2.sql.</summary>
public sealed class UserBranchConfiguration : IEntityTypeConfiguration<UserBranch>
{
    public void Configure(EntityTypeBuilder<UserBranch> builder)
    {
        builder.ToTable("UserBranch");

        builder.HasKey(x => new { x.UserId, x.BranchId }).HasName("PK_UserBranch");

        builder.Property(x => x.IsPrimary).IsRequired();
        builder.Property(x => x.GrantedOnUtc).IsRequired();
        builder.Property(x => x.GrantedBy).HasMaxLength(100);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserBranch_User_UserId");

        // BranchId is Organization.Branch, id only. No FK, no navigation.

        // One primary branch per user, enforced where two concurrent writers
        // cannot both win. Application code catches 2601/2627 and returns 409;
        // it does not read first and hope (03 §1 rule 6).
        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[IsPrimary] = 1")
            .HasDatabaseName("UX_UserBranch_OnePrimary");
    }
}
