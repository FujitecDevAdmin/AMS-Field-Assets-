using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[CustomFieldOption]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class CustomFieldOptionConfiguration : IEntityTypeConfiguration<CustomFieldOption>
{
    public void Configure(EntityTypeBuilder<CustomFieldOption> builder)
    {
        builder.ToTable("CustomFieldOption");

        builder.HasKey(x => x.Id).HasName("PK_CustomFieldOption");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.CustomFieldDefinitionId).IsRequired();
        builder.Property(x => x.OptionValue).HasMaxLength(150).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<CustomFieldDefinition>()
            .WithMany()
            .HasForeignKey(x => x.CustomFieldDefinitionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_CustomFieldOption_CustomFieldDefinition_CustomFieldDefinitionId");

        builder.HasIndex(x => new { x.CustomFieldDefinitionId, x.OptionValue })
            .IsUnique()
            .HasDatabaseName("UX_CustomFieldOption_Value");
    }
}
