using AMS.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Notifications.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Notifications].[EmailOutbox]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class EmailOutboxConfiguration : IEntityTypeConfiguration<EmailOutbox>
{
    public void Configure(EntityTypeBuilder<EmailOutbox> builder)
    {
        builder.ToTable("EmailOutbox");

        builder.HasKey(x => x.Id).HasName("PK_EmailOutbox");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ToAddress).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CcAddress).HasMaxLength(1000);
        builder.Property(x => x.Subject).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.IsHtml).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AttemptCount).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(500);
        builder.Property(x => x.SourceType).HasMaxLength(40);
        builder.Property(x => x.CreatedOnUtc).IsRequired();

        builder.HasIndex(x => new { x.Status, x.CreatedOnUtc })
            .HasFilter("[Status] = 'Pending'")
            .HasDatabaseName("IX_EmailOutbox_PendingOldest");

        builder.HasIndex(x => new { x.SourceType, x.SourceId })
            .HasDatabaseName("IX_EmailOutbox_Source");
    }
}
