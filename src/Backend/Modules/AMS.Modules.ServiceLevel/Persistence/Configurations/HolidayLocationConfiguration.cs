using AMS.Modules.ServiceLevel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceLevel.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceLevel].[HolidayLocation]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class HolidayLocationConfiguration : IEntityTypeConfiguration<HolidayLocation>
{
    public void Configure(EntityTypeBuilder<HolidayLocation> builder)
    {
        builder.ToTable("HolidayLocation");

        builder.HasKey(x => new { x.HolidayCalendarId, x.LocationId }).HasName("PK_HolidayLocation");

        builder.Property(x => x.HolidayCalendarId).IsRequired();
        builder.Property(x => x.LocationId).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);

        builder.HasOne<HolidayCalendar>()
            .WithMany()
            .HasForeignKey(x => x.HolidayCalendarId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_HolidayLocation_HolidayCalendar_HolidayCalendarId");

        builder.HasIndex(x => x.LocationId)
            .HasDatabaseName("IX_HolidayLocation_LocationId");
    }
}
