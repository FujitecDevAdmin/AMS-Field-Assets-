using AMS.Modules.ServiceLevel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceLevel.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceLevel].[HolidayCalendar]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class HolidayCalendarConfiguration : IEntityTypeConfiguration<HolidayCalendar>
{
    public void Configure(EntityTypeBuilder<HolidayCalendar> builder)
    {
        builder.ToTable("HolidayCalendar", table =>
        {
            table.HasCheckConstraint("CK_HolidayCalendar_Type", "([HolidayType] IN (N'Government', N'Festival', N'Regional', N'Optional'))");
            table.HasCheckConstraint("CK_HolidayCalendar_Recurrence", "([IsRecurringAnnually] = 0 OR ([RecurrenceMonth] BETWEEN 1 AND 12 AND [RecurrenceDay] >= 1 AND [RecurrenceDay] <= CASE WHEN [RecurrenceMonth] IN (4, 6, 9, 11) THEN 30 WHEN [RecurrenceMonth] = 2 THEN 29 ELSE 31 END))");
            table.HasCheckConstraint("CK_HolidayCalendar_YearMatchesDate", "([HolidayYear] = YEAR([HolidayDate]))");
            table.HasCheckConstraint("CK_HolidayCalendar_Year", "([HolidayYear] BETWEEN 2000 AND 2100)");
        });

        builder.HasKey(x => x.Id).HasName("PK_HolidayCalendar");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.HolidayName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.HolidayDate).IsRequired();
        builder.Property(x => x.HolidayYear).IsRequired();
        builder.Property(x => x.HolidayType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AppliesToAllLocations).IsRequired();
        builder.Property(x => x.IsRecurringAnnually).IsRequired().HasDefaultValueSql("0", "DF_HolidayCalendar_IsRecurring").ValueGeneratedNever();
        builder.Property(x => x.Remarks).HasMaxLength(300);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => new { x.HolidayYear, x.HolidayDate })
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_HolidayCalendar_YearDate");

        builder.HasIndex(x => new { x.RecurrenceMonth, x.RecurrenceDay })
            .HasFilter("[IsRecurringAnnually] = 1")
            .HasDatabaseName("IX_HolidayCalendar_Recurring");
    }
}
