namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestApprovalInstance]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class RequestApprovalInstance
{
    public long Id { get; set; }

    public int ServiceRequestId { get; set; }

    public int ApprovalWorkflowId { get; set; }

    public required string WorkflowNameSnapshot { get; set; }

    public int WorkflowVersion { get; set; }

    public required string Status { get; set; }

    public int? CurrentStageNumber { get; set; }

    public int SubmittedByUserId { get; set; }

    public DateTime SubmittedOnUtc { get; set; }

    public DateTime? CompletedOnUtc { get; set; }

    public DateTime? CancelledOnUtc { get; set; }

    public int? CancelledByUserId { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
