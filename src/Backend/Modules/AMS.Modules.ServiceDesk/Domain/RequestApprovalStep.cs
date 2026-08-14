namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestApprovalStep]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class RequestApprovalStep
{
    public long Id { get; set; }

    public long RequestApprovalInstanceId { get; set; }

    public int ApprovalWorkflowStageId { get; set; }

    public int StageNumber { get; set; }

    public required string StageNameSnapshot { get; set; }

    public required string ApprovalModeSnapshot { get; set; }

    public required string Status { get; set; }

    public DateTime? ActivatedOnUtc { get; set; }

    public DateTime? DueOnUtc { get; set; }

    public DateTime? CompletedOnUtc { get; set; }

    public string? OutcomeRemarks { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
