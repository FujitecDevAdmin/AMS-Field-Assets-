using AMS.Modules.DataImport.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.DataImport.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[DataImport].[ImportError]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class ImportErrorConfiguration : IEntityTypeConfiguration<ImportError>
{
    public void Configure(EntityTypeBuilder<ImportError> builder)
    {
        builder.ToTable("ImportError", table =>
        {
            table.HasCheckConstraint("CK_ImportError_RowNumber", "([RowNumber] > 0)");
        });

        builder.HasKey(x => x.Id).HasName("PK_ImportError");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ImportBatchId).IsRequired();
        builder.Property(x => x.RowNumber).IsRequired();
        builder.Property(x => x.ColumnName).HasMaxLength(128);
        builder.Property(x => x.RawValue).HasMaxLength(500);
        builder.Property(x => x.ErrorCode).HasMaxLength(60).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsResolved).IsRequired().HasDefaultValueSql("0", "DF_ImportError_IsResolved").ValueGeneratedNever();
        builder.Property(x => x.RecordedOnUtc).IsRequired();

        builder.HasOne<ImportBatch>()
            .WithMany()
            .HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ImportError_ImportBatch_ImportBatchId");

        builder.HasIndex(x => new { x.ImportBatchId, x.RowNumber })
            .HasDatabaseName("IX_ImportError_Batch");
    }
}
