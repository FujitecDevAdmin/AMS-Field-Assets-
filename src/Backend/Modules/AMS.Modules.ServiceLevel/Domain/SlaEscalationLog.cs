namespace AMS.Modules.ServiceLevel.Domain;

/// <summary>
/// Mirrors <c>[ServiceLevel].[SlaEscalationLog]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class SlaEscalationLog
{
    public long Id { get; set; }

    public int ServiceRequestId { get; set; }

    public int SlaEscalationId { get; set; }

    public required string EscalationType { get; set; }

    public int Level { get; set; }

    public required string SentTo { get; set; }

    public required string Channel { get; set; }

    public long? EmailOutboxId { get; set; }

    public required string Outcome { get; set; }

    public string? FailureReason { get; set; }

    public DateTime FiredOnUtc { get; set; }
}
