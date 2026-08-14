namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestApprovalParticipant]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class RequestApprovalParticipant
{
    public long Id { get; set; }

    public long RequestApprovalStepId { get; set; }

    public int ApproverRuleId { get; set; }

    public int? ApproverUserId { get; set; }

    public int? ApproverEmployeeId { get; set; }

    public required string ApproverNameSnapshot { get; set; }

    public required string ApproverEmailSnapshot { get; set; }

    public bool IsRequired { get; set; }

    public required string ParticipantStatus { get; set; }

    public int? DelegatedToUserId { get; set; }

    public DateTime? DelegatedOnUtc { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
