using AMS.Modules.Discovery.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Discovery.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Discovery].[AssetHealth]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetHealthConfiguration : IEntityTypeConfiguration<AssetHealth>
{
    public void Configure(EntityTypeBuilder<AssetHealth> builder)
    {
        builder.ToTable("AssetHealth");

        builder.HasKey(x => x.AssetId).HasName("PK_AssetHealth");
        builder.Property(x => x.AssetId).ValueGeneratedNever();

        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.Hostname).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CpuPercent).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.MemoryPercent).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.SystemDrivePercent).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.BatteryHealthPercent).HasPrecision(5, 2);
        builder.Property(x => x.UptimeHours).IsRequired();
        builder.Property(x => x.LoggedInUser).HasMaxLength(150);
        builder.Property(x => x.LastSeenOnUtc).IsRequired();

        builder.HasIndex(x => x.LastSeenOnUtc)
            .HasDatabaseName("IX_AssetHealth_LastSeen");
    }
}
