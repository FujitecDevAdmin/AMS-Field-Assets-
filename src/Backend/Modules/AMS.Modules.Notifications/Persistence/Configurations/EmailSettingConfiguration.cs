using AMS.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Notifications.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Notifications].[EmailSetting]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class EmailSettingConfiguration : IEntityTypeConfiguration<EmailSetting>
{
    public void Configure(EntityTypeBuilder<EmailSetting> builder)
    {
        builder.ToTable("EmailSetting");

        builder.HasKey(x => x.Id).HasName("PK_EmailSetting");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ProfileName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Host).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Port).IsRequired();
        builder.Property(x => x.UseSsl).IsRequired();
        builder.Property(x => x.FromAddress).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(200);
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => x.ProfileName)
            .IsUnique()
            .HasDatabaseName("UX_EmailSetting_Name");

        builder.HasIndex(x => x.IsDefault)
            .IsUnique()
            .HasFilter("[IsDefault] = 1")
            .HasDatabaseName("UX_EmailSetting_OneDefault");
    }
}
