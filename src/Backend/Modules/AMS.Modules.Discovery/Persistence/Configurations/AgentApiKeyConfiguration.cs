using AMS.Modules.Discovery.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Discovery.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Discovery].[AgentApiKey]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AgentApiKeyConfiguration : IEntityTypeConfiguration<AgentApiKey>
{
    public void Configure(EntityTypeBuilder<AgentApiKey> builder)
    {
        builder.ToTable("AgentApiKey");

        builder.HasKey(x => x.Id).HasName("PK_AgentApiKey");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.KeyName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.KeyPrefix).HasMaxLength(12).IsRequired();
        builder.Property(x => x.KeyHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => x.KeyPrefix)
            .HasDatabaseName("IX_AgentApiKey_Prefix");
    }
}
