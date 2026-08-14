namespace AMS.Modules.ServiceLevel.Features.SearchSlaPolicies;

/// <summary>
/// The policies, most urgent priority first.
/// </summary>
/// <param name="Rows">Each policy with the escalations configured against it.</param>
public sealed record SearchSlaPoliciesResponse(
    IReadOnlyList<SearchSlaPoliciesResponse.Row> Rows)
{
    /// <summary>One policy.</summary>
    /// <param name="Id">The policy.</param>
    /// <param name="PolicyName">What it is called.</param>
    /// <param name="Description">What it is for.</param>
    /// <param name="Priority">The priority it covers.</param>
    /// <param name="ResponseTargetMinutes">How long somebody has to reply.</param>
    /// <param name="ResolutionTargetMinutes">How long to fix it.</param>
    /// <param name="RespectOperationalHours">
    /// Whether the branch's opening hours apply. A Critical policy often turns
    /// all three off: a production outage does not wait for Monday.
    /// </param>
    /// <param name="RespectHolidays">Whether holidays stop the clock.</param>
    /// <param name="RespectWeekends">Whether weekends do.</param>
    /// <param name="NearDueWarningMinutes">How long before the deadline to warn.</param>
    /// <param name="IsActive">Whether tickets are judged by it.</param>
    /// <param name="Escalations">Who is told, and when.</param>
    public sealed record Row(
        int Id,
        string PolicyName,
        string? Description,
        string Priority,
        int ResponseTargetMinutes,
        int ResolutionTargetMinutes,
        bool RespectOperationalHours,
        bool RespectHolidays,
        bool RespectWeekends,
        int NearDueWarningMinutes,
        bool IsActive,
        IReadOnlyList<Escalation> Escalations);

    /// <summary>One rung of the ladder.</summary>
    /// <param name="Id">The escalation.</param>
    /// <param name="EscalationType">Response or Resolution.</param>
    /// <param name="Level">1 through 4.</param>
    /// <param name="ThresholdPercent">Additive to the target: 100 is the due time.</param>
    /// <param name="RecipientType">Who is told.</param>
    /// <param name="RecipientAddress">Where, when the recipient is a fixed address.</param>
    /// <param name="Channel">Email, InApp or Both.</param>
    /// <param name="IsEnabled">Whether it fires.</param>
    public sealed record Escalation(
        int Id,
        string EscalationType,
        int Level,
        int ThresholdPercent,
        string RecipientType,
        string? RecipientAddress,
        string Channel,
        bool IsEnabled);
}
