using AMS.Modules.DataImport.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.DataImport.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[DataImport].[ImportBatch]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("ImportBatch", table =>
        {
            table.HasCheckConstraint("CK_ImportBatch_Status", "([Status] IN (N'Running', N'Rehearsed', N'Committed', N'Failed', N'Cancelled'))");
            table.HasCheckConstraint("CK_ImportBatch_Counts", "([TotalRows] >= 0 AND [SucceededRows] >= 0 AND [FailedRows] >= 0 AND [SucceededRows] + [FailedRows] <= [TotalRows])");
        });

        builder.HasKey(x => x.Id).HasName("PK_ImportBatch");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.BatchNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ImportType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.FilePath).HasMaxLength(400);
        builder.Property(x => x.FileHash).HasMaxLength(128);
        builder.Property(x => x.IsDryRun).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.TotalRows).IsRequired().HasDefaultValueSql("0", "DF_ImportBatch_TotalRows").ValueGeneratedNever();
        builder.Property(x => x.SucceededRows).IsRequired().HasDefaultValueSql("0", "DF_ImportBatch_SucceededRows").ValueGeneratedNever();
        builder.Property(x => x.FailedRows).IsRequired().HasDefaultValueSql("0", "DF_ImportBatch_FailedRows").ValueGeneratedNever();
        builder.Property(x => x.ImportedByUserId).IsRequired();
        builder.Property(x => x.StartedOnUtc).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => x.BatchNumber)
            .IsUnique()
            .HasDatabaseName("UX_ImportBatch_Number");

        builder.HasIndex(x => new { x.ImportType, x.StartedOnUtc })
            .HasDatabaseName("IX_ImportBatch_TypeRecent");
    }
}
