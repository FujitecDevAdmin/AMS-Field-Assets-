using AMS.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Organization.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Organization].[Branch]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branch");

        builder.HasKey(x => x.Id).HasName("PK_Branch");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.BranchCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.BranchName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired().HasDefaultValueSql("N'India Standard Time'", "DF_Branch_TimeZoneId").ValueGeneratedNever();
        builder.Property(x => x.IsHeadOffice).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<Region>()
            .WithMany()
            .HasForeignKey(x => x.RegionId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Branch_Region_RegionId");

        builder.HasIndex(x => x.BranchCode)
            .IsUnique()
            .HasDatabaseName("UX_Branch_Code");

        builder.HasIndex(x => x.IsHeadOffice)
            .IsUnique()
            .HasFilter("[IsHeadOffice] = 1")
            .HasDatabaseName("UX_Branch_OneHeadOffice");

        builder.HasIndex(x => x.RegionId)
            .HasDatabaseName("IX_Branch_RegionId");
    }
}
