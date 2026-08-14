using AMS.Modules.ServiceLevel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceLevel.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceLevel].[LocationSaturdayRule]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class LocationSaturdayRuleConfiguration : IEntityTypeConfiguration<LocationSaturdayRule>
{
    public void Configure(EntityTypeBuilder<LocationSaturdayRule> builder)
    {
        builder.ToTable("LocationSaturdayRule", table =>
        {
            table.HasCheckConstraint("CK_LocationSaturdayRule_Occurrence", "([Occurrence] BETWEEN 1 AND 5)");
        });

        builder.HasKey(x => x.Id).HasName("PK_LocationSaturdayRule");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.LocationOperationalHourId).IsRequired();
        builder.Property(x => x.Occurrence).IsRequired();
        builder.Property(x => x.IsWorking).IsRequired();

        builder.HasOne<LocationOperationalHour>()
            .WithMany()
            .HasForeignKey(x => x.LocationOperationalHourId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_LocationSaturdayRule_LocationOperationalHour_LocationOperationalHourId");

        builder.HasIndex(x => new { x.LocationOperationalHourId, x.Occurrence })
            .IsUnique()
            .HasDatabaseName("UX_LocationSaturdayRule_Occurrence");
    }
}
