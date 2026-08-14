using AMS.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Notifications.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Notifications].[Notification]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notification");

        builder.HasKey(x => x.Id).HasName("PK_Notification");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Text).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DeepLink).HasMaxLength(200);
        builder.Property(x => x.IsRead).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.CreatedOnUtc })
            .HasFilter("[IsRead] = 0")
            .HasDatabaseName("IX_Notification_UserUnread");
    }
}
