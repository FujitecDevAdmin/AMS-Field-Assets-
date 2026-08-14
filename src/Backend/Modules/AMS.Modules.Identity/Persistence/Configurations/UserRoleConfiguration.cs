using AMS.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Identity.Persistence.Configurations;

/// <summary>Mirrors <c>[Identity].[UserRole]</c> in AMS_Consolidated_Design_v2.sql.</summary>
public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRole");

        builder.HasKey(x => new { x.UserId, x.RoleId }).HasName("PK_UserRole");

        builder.Property(x => x.GrantedOnUtc).IsRequired();
        builder.Property(x => x.GrantedBy).HasMaxLength(100);

        // Both ends are in THIS schema, so these are real foreign keys.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserRole_User_UserId");

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserRole_Role_RoleId");

        builder.HasIndex(x => x.RoleId).HasDatabaseName("IX_UserRole_RoleId");
    }
}
