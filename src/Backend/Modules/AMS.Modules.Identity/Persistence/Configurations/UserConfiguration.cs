using AMS.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Identity.Persistence.Configurations;

/// <summary>Mirrors <c>[Identity].[User]</c> in AMS_Consolidated_Design_v2.sql.</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("User");

        builder.HasKey(x => x.Id).HasName("PK_User");

        builder.Property(x => x.Username).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256);

        builder.Property(x => x.MustChangePassword).IsRequired();
        builder.Property(x => x.IsLocked).IsRequired();
        builder.Property(x => x.FailedLoginAttempts).IsRequired();
        builder.Property(x => x.HasAllBranches).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.MfaEnabled).IsRequired();
        builder.Property(x => x.MfaEnrollmentRequired).IsRequired();

        // varbinary(max). Data Protection owns the contents; EF only stores bytes.
        builder.Property(x => x.MfaSecretEncrypted).HasColumnType("varbinary(max)");

        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.Username)
            .IsUnique()
            .HasDatabaseName("UX_User_Username");

        // Filtered: many users have no employee record, and without the filter
        // the second NULL would collide with the first.
        builder.HasIndex(x => x.EmployeeId)
            .IsUnique()
            .HasFilter("[EmployeeId] IS NOT NULL")
            .HasDatabaseName("UX_User_Employee");

        // EmployeeId is Organization.Employee, id only. No FK, no navigation.
    }
}
