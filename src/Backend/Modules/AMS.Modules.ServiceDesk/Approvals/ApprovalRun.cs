using AMS.Modules.ServiceDesk.Domain;

namespace AMS.Modules.ServiceDesk.Approvals;

/// <summary>
/// The rules for how a run moves: when a level is settled, and what settling it
/// does to the run.
/// </summary>
/// <remarks>
/// Pure functions over rows the caller has already loaded. Kept apart from the
/// handlers because the same rules are needed by the decision slice today and
/// by the reminder/escalation worker later, and two implementations of
/// "does one rejection sink the level" is one too many.
/// </remarks>
public static class ApprovalRun
{
    /// <summary>What a level's participants add up to.</summary>
    /// <param name="mode">Any or All.</param>
    /// <param name="participants">Everybody asked at this level.</param>
    /// <returns>
    /// The status the step should now hold, or null while it is still waiting
    /// on somebody.
    /// </returns>
    /// <remarks>
    /// A rejection settles the level under BOTH modes. The design script says
    /// so for Any and for All, and it is the only reading that makes sense: a
    /// level exists to let somebody say no.
    /// </remarks>
    public static string? Settle(string mode, IReadOnlyList<RequestApprovalParticipant> participants)
    {
        ArgumentNullException.ThrowIfNull(participants);

        if (participants.Any(p => p.ParticipantStatus == ParticipantStatus.Rejected))
        {
            return ApprovalStepStatus.Rejected;
        }

        var approved = participants.Count(p => p.ParticipantStatus == ParticipantStatus.Approved);

        if (mode == ApprovalMode.Any)
        {
            return approved > 0 ? ApprovalStepStatus.Approved : null;
        }

        // All: every REQUIRED approver must have approved. An optional
        // participant who never answers cannot hold up the level - that is
        // what IsRequired = false means, and a level that waited for them
        // anyway would make the flag a decoration.
        var outstanding = participants.Count(p =>
            p.IsRequired
            && p.ParticipantStatus is ParticipantStatus.Waiting or ParticipantStatus.Pending);

        return outstanding == 0 && approved > 0 ? ApprovalStepStatus.Approved : null;
    }

    /// <summary>
    /// Activates a step: its turn has come.
    /// </summary>
    /// <remarks>
    /// <c>CK_RequestApprovalStep_Activation</c> requires an activation time on
    /// anything that is not Waiting, Cancelled or Skipped, so the two are set
    /// together and never apart. The due time is computed here rather than by
    /// a worker, because the worker that sends reminders needs it to already
    /// exist — IX_RequestApprovalStep_Due is filtered on Pending.
    /// </remarks>
    public static void Activate(
        RequestApprovalStep step,
        ApprovalWorkflowStage stage,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(stage);

        step.Status = ApprovalStepStatus.Pending;
        step.ActivatedOnUtc = now;
        step.DueOnUtc = stage.DueAfterMinutes is { } minutes ? now.AddMinutes(minutes) : null;
        step.ModifiedOnUtc = now;
    }
}
