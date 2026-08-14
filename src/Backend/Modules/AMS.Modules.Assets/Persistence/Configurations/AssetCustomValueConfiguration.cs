using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[AssetCustomValue]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetCustomValueConfiguration : IEntityTypeConfiguration<AssetCustomValue>
{
    public void Configure(EntityTypeBuilder<AssetCustomValue> builder)
    {
        builder.ToTable("AssetCustomValue");

        builder.HasKey(x => x.Id).HasName("PK_AssetCustomValue");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.CustomFieldDefinitionId).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(1000);
        builder.Property(x => x.ValueNumber).HasPrecision(18, 4);
        builder.Property(x => x.UpdatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_AssetCustomValue_Asset_AssetId");

        builder.HasOne<CustomFieldDefinition>()
            .WithMany()
            .HasForeignKey(x => x.CustomFieldDefinitionId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetCustomValue_CustomFieldDefinition_CustomFieldDefinitionId");

        builder.HasOne<CustomFieldOption>()
            .WithMany()
            .HasForeignKey(x => x.OptionId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetCustomValue_CustomFieldOption_OptionId");

        builder.HasIndex(x => new { x.AssetId, x.CustomFieldDefinitionId })
            .IsUnique()
            .HasDatabaseName("UX_AssetCustomValue_AssetField");

        builder.HasIndex(x => new { x.CustomFieldDefinitionId, x.ValueNumber })
            .HasDatabaseName("IX_AssetCustomValue_NumericLookup");

        builder.HasIndex(x => x.OptionId)
            .HasDatabaseName("IX_AssetCustomValue_OptionId");
    }
}
