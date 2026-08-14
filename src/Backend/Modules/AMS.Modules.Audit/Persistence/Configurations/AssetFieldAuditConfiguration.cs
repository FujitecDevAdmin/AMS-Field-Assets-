using AMS.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Audit.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Audit].[AssetFieldAudit]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetFieldAuditConfiguration : IEntityTypeConfiguration<AssetFieldAudit>
{
    public void Configure(EntityTypeBuilder<AssetFieldAudit> builder)
    {
        builder.ToTable("AssetFieldAudit");

        builder.HasKey(x => x.Id).HasName("PK_AssetFieldAudit");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FieldName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.OldValue).HasMaxLength(1024);
        builder.Property(x => x.NewValue).HasMaxLength(1024);
        builder.Property(x => x.ChangedOnUtc).IsRequired();
        builder.Property(x => x.ChangedBy).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => new { x.AssetId, x.ChangedOnUtc })
            .HasDatabaseName("IX_AFA_Asset");
    }
}
