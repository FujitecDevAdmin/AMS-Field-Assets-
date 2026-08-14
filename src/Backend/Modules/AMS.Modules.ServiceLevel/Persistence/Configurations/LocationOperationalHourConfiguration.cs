using AMS.Modules.ServiceLevel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceLevel.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceLevel].[LocationOperationalHour]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class LocationOperationalHourConfiguration : IEntityTypeConfiguration<LocationOperationalHour>
{
    public void Configure(EntityTypeBuilder<LocationOperationalHour> builder)
    {
        builder.ToTable("LocationOperationalHour", table =>
        {
            table.HasCheckConstraint("CK_LocationOperationalHour_Window", "([IsRoundTheClock] = 1 OR [StandardEndTime] > [StandardStartTime])");
            table.HasCheckConstraint("CK_LocationOperationalHour_BreakPair", "(([BreakStartTime] IS NULL AND [BreakEndTime] IS NULL) OR ([BreakStartTime] IS NOT NULL AND [BreakEndTime] IS NOT NULL AND [BreakEndTime] > [BreakStartTime]))");
            table.HasCheckConstraint("CK_LocationOperationalHour_BreakInside", "([IsRoundTheClock] = 1 OR [BreakStartTime] IS NULL OR ([BreakStartTime] >= [StandardStartTime] AND [BreakEndTime] <= [StandardEndTime]))");
            table.HasCheckConstraint("CK_LocationOperationalHour_DeferMinutes", "([DeferFinalMinutes] BETWEEN 0 AND 480)");
            table.IsTemporal(temporal =>
            {
                temporal.HasPeriodStart("SysStartTime");
                temporal.HasPeriodEnd("SysEndTime");
                temporal.UseHistoryTable("LocationOperationalHourHistory", "ServiceLevel");
            });
        });

        builder.HasKey(x => x.Id).HasName("PK_LocationOperationalHour");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.LocationId).IsRequired();
        builder.Property(x => x.IsRoundTheClock).IsRequired().HasDefaultValueSql("0", "DF_LocationOperationalHour_IsRoundTheClock").ValueGeneratedNever();
        builder.Property(x => x.StandardStartTime).IsRequired();
        builder.Property(x => x.StandardEndTime).IsRequired();
        builder.Property(x => x.DeferFinalMinutes).IsRequired().HasDefaultValueSql("30", "DF_LocationOperationalHour_DeferFinalMinutes").ValueGeneratedNever();
        builder.Property(x => x.DeferNewTicketsOnFriday).IsRequired().HasDefaultValueSql("0", "DF_LocationOperationalHour_DeferOnFriday").ValueGeneratedNever();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        // R2-22: the token for a system-versioned table. SysStartTime is history only.
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken().HasDefaultValueSql("NEWID()", "DF_LocationOperationalHour_ConcurrencyStamp");

        builder.HasIndex(x => x.LocationId)
            .IsUnique()
            .HasDatabaseName("UX_LocationOperationalHour_Location");
    }
}
