using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[CustomFieldDefinition]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class CustomFieldDefinitionConfiguration : IEntityTypeConfiguration<CustomFieldDefinition>
{
    public void Configure(EntityTypeBuilder<CustomFieldDefinition> builder)
    {
        builder.ToTable("CustomFieldDefinition", table =>
        {
            table.HasCheckConstraint("CK_CustomFieldDefinition_Type", "([FieldType] IN (N'Text', N'Number', N'Percentage', N'Date', N'Boolean', N'Dropdown'))");
            table.HasCheckConstraint("CK_CustomFieldDefinition_Range", "([MinValue] IS NULL OR [MaxValue] IS NULL OR [MaxValue] >= [MinValue])");
        });

        builder.HasKey(x => x.Id).HasName("PK_CustomFieldDefinition");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AssetTypeId).IsRequired();
        builder.Property(x => x.FieldName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.DisplayLabel).HasMaxLength(150).IsRequired();
        builder.Property(x => x.FieldType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.IsRequired).IsRequired();
        builder.Property(x => x.MinValue).HasPrecision(18, 4);
        builder.Property(x => x.MaxValue).HasPrecision(18, 4);
        builder.Property(x => x.ValidationRegex).HasMaxLength(300);
        builder.Property(x => x.DefaultValue).HasMaxLength(300);
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<AssetType>()
            .WithMany()
            .HasForeignKey(x => x.AssetTypeId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_CustomFieldDefinition_AssetType_AssetTypeId");

        builder.HasIndex(x => new { x.AssetTypeId, x.FieldName })
            .IsUnique()
            .HasDatabaseName("UX_CustomFieldDefinition_TypeField");
    }
}
