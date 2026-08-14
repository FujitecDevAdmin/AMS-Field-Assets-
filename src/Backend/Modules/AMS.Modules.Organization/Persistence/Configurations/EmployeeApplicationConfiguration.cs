using AMS.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Organization.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Organization].[EmployeeApplication]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class EmployeeApplicationConfiguration : IEntityTypeConfiguration<EmployeeApplication>
{
    public void Configure(EntityTypeBuilder<EmployeeApplication> builder)
    {
        builder.ToTable("EmployeeApplication");

        builder.HasKey(x => x.Id).HasName("PK_EmployeeApplication");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.EmployeeId).IsRequired();
        builder.Property(x => x.ApplicationId).IsRequired();
        builder.Property(x => x.ApplicationLoginId).HasMaxLength(100);
        builder.Property(x => x.GrantedOnUtc).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<Application>()
            .WithMany()
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_EmployeeApplication_Application_ApplicationId");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_EmployeeApplication_Employee_EmployeeId");

        builder.HasIndex(x => x.ApplicationId)
            .HasDatabaseName("IX_EmployeeApplication_ApplicationId");

        builder.HasIndex(x => new { x.EmployeeId, x.ApplicationId })
            .IsUnique()
            .HasFilter("[RevokedOnUtc] IS NULL")
            .HasDatabaseName("UX_EmployeeApplication_OneActive");
    }
}
