using AMS.Modules.Discovery.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Discovery.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Discovery].[DiscoveredDevice]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class DiscoveredDeviceConfiguration : IEntityTypeConfiguration<DiscoveredDevice>
{
    public void Configure(EntityTypeBuilder<DiscoveredDevice> builder)
    {
        builder.ToTable("DiscoveredDevice");

        builder.HasKey(x => x.Id).HasName("PK_DiscoveredDevice");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.Hostname).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.Manufacturer).HasMaxLength(150);
        builder.Property(x => x.Model).HasMaxLength(150);
        builder.Property(x => x.OperatingSystem).HasMaxLength(150);
        builder.Property(x => x.MacAddress).HasMaxLength(50);
        builder.Property(x => x.RawPayloadJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.FirstSeenOnUtc).IsRequired();
        builder.Property(x => x.LastSeenOnUtc).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_DiscoveredDevice_Status");

        builder.HasIndex(x => new { x.Hostname, x.SerialNumber })
            .IsUnique()
            .HasFilter("[SerialNumber] IS NOT NULL")
            .HasDatabaseName("UX_DiscoveredDevice_Machine");
    }
}
