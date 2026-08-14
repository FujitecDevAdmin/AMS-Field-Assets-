namespace AMS.Modules.Discovery.Domain;

/// <summary>
/// Mirrors <c>[Discovery].[AgentApiKey]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AgentApiKey
{
    public int Id { get; set; }

    public required string KeyName { get; set; }

    public required string KeyPrefix { get; set; }

    public required string KeyHash { get; set; }

    public DateTime? LastUsedOnUtc { get; set; }

    public DateTime? RevokedOnUtc { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
