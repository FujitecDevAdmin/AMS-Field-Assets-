using AMS.Modules.Contracts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Contracts.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Contracts].[ContractDocument]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class ContractDocumentConfiguration : IEntityTypeConfiguration<ContractDocument>
{
    public void Configure(EntityTypeBuilder<ContractDocument> builder)
    {
        builder.ToTable("ContractDocument");

        builder.HasKey(x => x.Id).HasName("PK_ContractDocument");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ContractId).IsRequired();
        builder.Property(x => x.FilePath).HasMaxLength(400).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(260);
        builder.Property(x => x.ContentType).HasMaxLength(120);
        builder.Property(x => x.UploadedOnUtc).IsRequired();

        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ContractDocument_Contract_ContractId");

        builder.HasIndex(x => x.ContractId)
            .HasDatabaseName("IX_ContractDocument_ContractId");
    }
}
