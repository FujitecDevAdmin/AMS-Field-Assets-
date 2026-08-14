namespace AMS.Modules.ServiceLevel.Features.SearchEscalationLog;

/// <summary>
/// What was sent, to whom, and whether it arrived.
/// </summary>
/// <param name="Rows">The log, most recent first.</param>
public sealed record SearchEscalationLogResponse(
    IReadOnlyList<SearchEscalationLogResponse.Row> Rows)
{
    /// <summary>One escalation that fired.</summary>
    /// <param name="Id">The log row.</param>
    /// <param name="ServiceRequestId">The ticket it was about.</param>
    /// <param name="SlaEscalationId">The rung that fired.</param>
    /// <param name="EscalationType">Response or Resolution.</param>
    /// <param name="Level">Which rung.</param>
    /// <param name="SentTo">The address it went to, as it was then.</param>
    /// <param name="Channel">How it was sent.</param>
    /// <param name="Outcome">Queued, Sent, Failed or Skipped.</param>
    /// <param name="FailureReason">Why, when it failed.</param>
    /// <param name="FiredOnUtc">When.</param>
    public sealed record Row(
        long Id,
        int ServiceRequestId,
        int SlaEscalationId,
        string EscalationType,
        int Level,
        string SentTo,
        string Channel,
        string Outcome,
        string? FailureReason,
        DateTime FiredOnUtc);
}
