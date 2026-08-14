namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[ApprovalWorkflowDefinition]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class ApprovalWorkflowDefinition
{
    public int Id { get; set; }

    public required string WorkflowName { get; set; }

    public int VersionNumber { get; set; }

    public string? Description { get; set; }

    public int? ServiceTemplateId { get; set; }

    public int? LocationId { get; set; }

    public string? Priority { get; set; }

    public bool IsDefault { get; set; }

    public bool IsPublished { get; set; }

    public bool IsActive { get; set; }

    public DateTime? EffectiveFromUtc { get; set; }

    public DateTime? EffectiveToUtc { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
