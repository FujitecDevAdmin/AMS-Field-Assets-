namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[ApprovalStageApproverRule]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class ApprovalStageApproverRule
{
    public int Id { get; set; }

    public int ApprovalWorkflowStageId { get; set; }

    public required string ResolverType { get; set; }

    public int? ResolverUserId { get; set; }

    public int? ResolverRoleId { get; set; }

    public string? ResolverCapabilityName { get; set; }

    public string? ResolverEmail { get; set; }

    public string? DisplayName { get; set; }

    /// <summary>Defaults to <c>1</c>, as <c>DF_ApprovalStageApproverRule_IsRequired</c> does.</summary>
    public bool IsRequired { get; set; } = true;

    public bool IsEnabled { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
