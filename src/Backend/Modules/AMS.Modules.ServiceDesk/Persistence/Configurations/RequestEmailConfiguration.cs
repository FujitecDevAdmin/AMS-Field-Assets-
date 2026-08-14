using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestEmail]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class RequestEmailConfiguration : IEntityTypeConfiguration<RequestEmail>
{
    public void Configure(EntityTypeBuilder<RequestEmail> builder)
    {
        builder.ToTable("RequestEmail", table =>
        {
            table.HasCheckConstraint("CK_RequestEmail_Status", "([Status] IN (N'Queued', N'Sent', N'Failed'))");
            table.HasCheckConstraint("CK_RequestEmail_Direction", "([Direction] IN (N'Outbound', N'Inbound'))");
            table.HasCheckConstraint("CK_RequestEmail_SentBy", "([Direction] = N'Inbound' OR [SentByUserId] IS NOT NULL)");
        });

        builder.HasKey(x => x.Id).HasName("PK_RequestEmail");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ServiceRequestId).IsRequired();
        builder.Property(x => x.Direction).HasMaxLength(10).IsRequired().HasDefaultValueSql("N'Outbound'", "DF_RequestEmail_Direction").ValueGeneratedNever();
        builder.Property(x => x.ToAddresses).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.CcAddresses).HasMaxLength(1000);
        builder.Property(x => x.Subject).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.IsHtml).IsRequired().HasDefaultValueSql("1", "DF_RequestEmail_IsHtml").ValueGeneratedNever();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(500);
        builder.Property(x => x.QueuedOnUtc).IsRequired();

        builder.HasOne<ServiceRequest>()
            .WithMany()
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RequestEmail_ServiceRequest_ServiceRequestId");

        builder.HasIndex(x => new { x.ServiceRequestId, x.QueuedOnUtc })
            .HasDatabaseName("IX_RequestEmail_Request");
    }
}
