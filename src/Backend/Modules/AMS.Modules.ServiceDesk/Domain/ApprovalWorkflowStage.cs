namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[ApprovalWorkflowStage]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class ApprovalWorkflowStage
{
    public int Id { get; set; }

    public int ApprovalWorkflowId { get; set; }

    public int StageNumber { get; set; }

    public required string StageName { get; set; }

    public required string ApprovalMode { get; set; }

    public int? DueAfterMinutes { get; set; }

    public int? ReminderAfterMinutes { get; set; }

    public int? ReminderRepeatMinutes { get; set; }

    public int? EscalateAfterMinutes { get; set; }

    public bool AllowDelegation { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
