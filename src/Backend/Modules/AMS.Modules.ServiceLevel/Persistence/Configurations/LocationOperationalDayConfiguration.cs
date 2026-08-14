using AMS.Modules.ServiceLevel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceLevel.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceLevel].[LocationOperationalDay]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class LocationOperationalDayConfiguration : IEntityTypeConfiguration<LocationOperationalDay>
{
    public void Configure(EntityTypeBuilder<LocationOperationalDay> builder)
    {
        builder.ToTable("LocationOperationalDay", table =>
        {
            table.HasCheckConstraint("CK_LocationOperationalDay_DayOfWeek", "([DayOfWeek] BETWEEN 0 AND 6)");
            table.HasCheckConstraint("CK_LocationOperationalDay_DayType", "([DayType] IN (N'Standard', N'Custom', N'TwentyFourHour'))");
            table.HasCheckConstraint("CK_LocationOperationalDay_CustomTimes", "([DayType] <> N'Custom' OR ([StartTime] IS NOT NULL AND [EndTime] IS NOT NULL AND [EndTime] > [StartTime]))");
            table.HasCheckConstraint("CK_LocationOperationalDay_CustomBreak", "(([BreakStartTime] IS NULL AND [BreakEndTime] IS NULL) OR ([BreakStartTime] IS NOT NULL AND [BreakEndTime] IS NOT NULL AND [BreakEndTime] > [BreakStartTime]))");
        });

        builder.HasKey(x => x.Id).HasName("PK_LocationOperationalDay");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.LocationOperationalHourId).IsRequired();
        builder.Property(x => x.DayOfWeek).IsRequired();
        builder.Property(x => x.IsWorkingDay).IsRequired();
        builder.Property(x => x.DayType).HasMaxLength(20).IsRequired();

        builder.HasOne<LocationOperationalHour>()
            .WithMany()
            .HasForeignKey(x => x.LocationOperationalHourId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_LocationOperationalDay_LocationOperationalHour_LocationOperationalHourId");

        builder.HasIndex(x => new { x.LocationOperationalHourId, x.DayOfWeek })
            .IsUnique()
            .HasDatabaseName("UX_LocationOperationalDay_Day");
    }
}
