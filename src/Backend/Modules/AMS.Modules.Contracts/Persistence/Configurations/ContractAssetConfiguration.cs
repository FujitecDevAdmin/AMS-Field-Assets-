using AMS.Modules.Contracts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Contracts.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Contracts].[ContractAsset]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class ContractAssetConfiguration : IEntityTypeConfiguration<ContractAsset>
{
    public void Configure(EntityTypeBuilder<ContractAsset> builder)
    {
        builder.ToTable("ContractAsset");

        builder.HasKey(x => x.Id).HasName("PK_ContractAsset");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ContractId).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.LinkedOnUtc).IsRequired();

        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ContractAsset_Contract_ContractId");

        builder.HasIndex(x => new { x.ContractId, x.AssetId })
            .IsUnique()
            .HasDatabaseName("UX_ContractAsset_NoDuplicates");
    }
}
