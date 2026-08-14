using AMS.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Organization.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Organization].[Location]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Location");

        builder.HasKey(x => x.Id).HasName("PK_Location");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.LocationCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LocationName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired().HasDefaultValueSql("N'India Standard Time'", "DF_Location_TimeZoneId").ValueGeneratedNever();
        builder.Property(x => x.IsHeadOffice).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<Region>()
            .WithMany()
            .HasForeignKey(x => x.RegionId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Location_Region_RegionId");

        builder.HasIndex(x => x.LocationCode)
            .IsUnique()
            .HasDatabaseName("UX_Location_Code");

        builder.HasIndex(x => x.IsHeadOffice)
            .IsUnique()
            .HasFilter("[IsHeadOffice] = 1")
            .HasDatabaseName("UX_Location_OneHeadOffice");

        builder.HasIndex(x => x.RegionId)
            .HasDatabaseName("IX_Location_RegionId");
    }
}
