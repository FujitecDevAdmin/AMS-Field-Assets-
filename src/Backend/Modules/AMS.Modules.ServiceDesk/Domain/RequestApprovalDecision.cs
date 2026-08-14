namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestApprovalDecision]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class RequestApprovalDecision
{
    public long Id { get; set; }

    public long RequestApprovalParticipantId { get; set; }

    public Guid ClientDecisionId { get; set; }

    public required string Decision { get; set; }

    public string? Remarks { get; set; }

    public int? ActedByUserId { get; set; }

    public required string ActedByEmailSnapshot { get; set; }

    public required string Source { get; set; }

    public DateTime DecidedOnUtc { get; set; }

    public string? SourceIpAddress { get; set; }

    public string? UserAgent { get; set; }
}
