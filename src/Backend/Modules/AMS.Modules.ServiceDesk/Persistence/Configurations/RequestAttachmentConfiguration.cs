using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestAttachment]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class RequestAttachmentConfiguration : IEntityTypeConfiguration<RequestAttachment>
{
    public void Configure(EntityTypeBuilder<RequestAttachment> builder)
    {
        builder.ToTable("RequestAttachment", table =>
        {
            table.HasCheckConstraint("CK_RequestAttachment_Type", "([AttachmentType] IN (N'Requester', N'Resolution', N'Email'))");
        });

        builder.HasKey(x => x.Id).HasName("PK_RequestAttachment");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ServiceRequestId).IsRequired();
        builder.Property(x => x.AttachmentType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.FilePath).HasMaxLength(400).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(260);
        builder.Property(x => x.ContentType).HasMaxLength(120);
        builder.Property(x => x.UploadedOnUtc).IsRequired();

        builder.HasOne<ServiceRequest>()
            .WithMany()
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RequestAttachment_ServiceRequest_ServiceRequestId");

        builder.HasOne<RequestEmail>()
            .WithMany()
            .HasForeignKey(x => x.RequestEmailId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_RequestAttachment_RequestEmail_RequestEmailId");

        builder.HasIndex(x => x.ServiceRequestId)
            .HasDatabaseName("IX_RequestAttachment_ServiceRequestId");
    }
}
