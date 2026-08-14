using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[ServiceTemplate]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class ServiceTemplateConfiguration : IEntityTypeConfiguration<ServiceTemplate>
{
    public void Configure(EntityTypeBuilder<ServiceTemplate> builder)
    {
        builder.ToTable("ServiceTemplate", table =>
        {
            table.HasCheckConstraint("CK_ServiceTemplate_Kind", "([RequestKind] IN (N'SupportTicket', N'AssetIssue', N'NewService'))");
            table.HasCheckConstraint("CK_ServiceTemplate_Priority", "([DefaultPriority] IN (N'Low', N'Medium', N'High', N'Critical'))");
        });

        builder.HasKey(x => x.Id).HasName("PK_ServiceTemplate");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.TemplateName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.RequestKind).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DefaultPriority).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SubjectTemplate).HasMaxLength(300).IsRequired();
        builder.Property(x => x.DescriptionTemplate).HasMaxLength(4000);
        builder.Property(x => x.RequiresAsset).IsRequired().HasDefaultValueSql("0", "DF_ServiceTemplate_RequiresAsset").ValueGeneratedNever();
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<RequestCategory>()
            .WithMany()
            .HasForeignKey(x => x.RequestCategoryId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ServiceTemplate_RequestCategory_RequestCategoryId");

        builder.HasOne<RequestSubCategory>()
            .WithMany()
            .HasForeignKey(x => x.RequestSubCategoryId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ServiceTemplate_RequestSubCategory_RequestSubCategoryId");

        builder.HasOne<SupportTeam>()
            .WithMany()
            .HasForeignKey(x => x.DefaultSupportTeamId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ServiceTemplate_SupportTeam_DefaultSupportTeamId");

        builder.HasIndex(x => x.TemplateName)
            .IsUnique()
            .HasDatabaseName("UX_ServiceTemplate_Name");
    }
}
