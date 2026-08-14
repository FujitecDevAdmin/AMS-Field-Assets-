using AMS.Modules.ServiceDesk.Domain;

namespace AMS.Modules.ServiceDesk.Approvals;

/// <summary>
/// When a waiting approval should be chased, and how many times it already has
/// been.
/// </summary>
/// <remarks>
/// <para>
/// Pure arithmetic over a stage's timers, kept apart from the worker that acts
/// on it so the rules can be stated and checked without a database, a clock or
/// an approval.
/// </para>
/// <para>
/// Counted from the activation time rather than tracked in a column, because a
/// counter is state that can drift and this is arithmetic that cannot. It also
/// means a worker switched off for a day comes back and sends the reminder that
/// is due NOW, not the four it missed.
/// </para>
/// </remarks>
public static class ApprovalSchedule
{
    /// <summary>
    /// Which reminder is due: 1 for the first, 2 for the next repeat, and so
    /// on. Zero when none is.
    /// </summary>
    public static int ReminderOccurrence(
        ApprovalWorkflowStage stage,
        DateTime activatedOnUtc,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(stage);

        // The columns are nullable, and null means "do not chase".
        if (stage.ReminderAfterMinutes is not { } first)
        {
            return 0;
        }

        var elapsed = (now - activatedOnUtc).TotalMinutes;

        if (elapsed < first)
        {
            return 0;
        }

        if (stage.ReminderRepeatMinutes is not { } repeat || repeat <= 0)
        {
            return 1;
        }

        return 1 + (int)((elapsed - first) / repeat);
    }

    /// <summary>Whether an escalation is due. Zero or one.</summary>
    /// <remarks>
    /// Measured from the DUE time, not from activation — the column says "after
    /// the step becomes due", and a stage with no due time can never become due,
    /// so it never escalates.
    ///
    /// It happens once. There is no repeat column for escalation, and telling
    /// somebody every hour that a thing is still stuck is how they stop reading
    /// it.
    /// </remarks>
    public static int EscalationOccurrence(
        RequestApprovalStep step,
        ApprovalWorkflowStage stage,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(stage);

        if (stage.EscalateAfterMinutes is not { } after || step.DueOnUtc is not { } due)
        {
            return 0;
        }

        return (now - due).TotalMinutes >= after ? 1 : 0;
    }
}
