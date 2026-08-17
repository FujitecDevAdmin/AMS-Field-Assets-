using AMS.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Organization.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Organization].[Employee]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employee", table =>
        {
            table.IsTemporal(temporal =>
            {
                temporal.HasPeriodStart("SysStartTime");
                temporal.HasPeriodEnd("SysEndTime");
                temporal.UseHistoryTable("EmployeeHistory", "Organization");
            });
        });

        builder.HasKey(x => x.Id).HasName("PK_Employee");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.EmployeeCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Phone).HasMaxLength(40);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        // R2-22: the token for a system-versioned table. SysStartTime is history only.
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken().HasDefaultValueSql("NEWID()", "DF_Employee_ConcurrencyStamp");

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Employee_Department_DepartmentId");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.ReportingManagerId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Employee_Employee_ReportingManagerId");

        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Employee_Branch_BranchId");

        builder.HasIndex(x => x.EmployeeCode)
            .IsUnique()
            .HasDatabaseName("UX_Employee_Code");

        builder.HasIndex(x => new { x.BranchId, x.FullName })
            .HasDatabaseName("IX_Employee_Branch");

        builder.HasIndex(x => x.DepartmentId)
            .HasDatabaseName("IX_Employee_DepartmentId");

        builder.HasIndex(x => x.ReportingManagerId)
            .HasDatabaseName("IX_Employee_ReportingManagerId");
    }
}
