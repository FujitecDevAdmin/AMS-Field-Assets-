using AMS.Modules.SapSync.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.SapSync.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[SapSync].[SapSyncLog]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class SapSyncLogConfiguration : IEntityTypeConfiguration<SapSyncLog>
{
    public void Configure(EntityTypeBuilder<SapSyncLog> builder)
    {
        builder.ToTable("SapSyncLog");

        builder.HasKey(x => x.Id).HasName("PK_SapSyncLog");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.Direction).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SyncType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Outcome).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.RecordsProcessed).IsRequired();
        builder.Property(x => x.RecordsFailed).IsRequired();
        builder.Property(x => x.SourceReference).HasMaxLength(100);
        builder.Property(x => x.StartedOnUtc).IsRequired();
        builder.Property(x => x.AttemptCount).IsRequired();

        builder.HasIndex(x => x.StartedOnUtc)
            .HasDatabaseName("IX_SapSyncLog_Recent");

        builder.HasIndex(x => new { x.Outcome, x.StartedOnUtc })
            .HasDatabaseName("IX_SapSyncLog_Failures");
    }
}
