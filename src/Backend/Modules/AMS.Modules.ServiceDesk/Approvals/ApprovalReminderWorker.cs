using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Approvals;

/// <summary>
/// Chases approvals nobody has acted on.
/// </summary>
/// <remarks>
/// <para>
/// The stage timer columns — <c>ReminderAfterMinutes</c>,
/// <c>ReminderRepeatMinutes</c>, <c>EscalateAfterMinutes</c> — have existed
/// since the schema was written, with a note saying they "support
/// reminder/escalation jobs without embedding timers in UI code". This is that
/// job.
/// </para>
/// <para>
/// It reads <c>IX_RequestApprovalStep_Due</c>, which is filtered on Pending
/// precisely so this query stays small as finished runs accumulate.
/// </para>
/// <para>
/// Everything it sends is idempotent by construction: the occurrence number is
/// part of the key, so a pass that runs twice in the same minute sends nothing
/// the second time, and a pass that is missed catches up rather than skipping.
/// </para>
/// </remarks>
public sealed class ApprovalReminderWorker(
    ServiceDeskDbContext db,
    ApprovalNotifications notifications,
    IClock clock)
{
    /// <summary>
    /// Chases everything that is due. Returns how many notifications were sent.
    /// </summary>
    /// <remarks>
    /// Callable directly, so it can be tested by moving a clock rather than by
    /// waiting. A worker that only runs on a timer is a worker nobody can test.
    /// </remarks>
    public async Task<int> RunAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;

        var due = await (
            from step in db.RequestApprovalSteps
            join instance in db.RequestApprovalInstances
                on step.RequestApprovalInstanceId equals instance.Id
            join stage in db.ApprovalWorkflowStages
                on step.ApprovalWorkflowStageId equals stage.Id
            join ticket in db.ServiceRequests on instance.ServiceRequestId equals ticket.Id
            where step.Status == ApprovalStepStatus.Pending
                && instance.Status == ApprovalInstanceStatus.Pending
                && step.ActivatedOnUtc != null
            select new { Step = step, Instance = instance, Stage = stage, Ticket = ticket })
            .ToListAsync(ct);

        var sent = 0;

        foreach (var row in due)
        {
            sent += await ChaseAsync(row.Instance, row.Step, row.Stage, row.Ticket, now, ct);
        }

        return sent;
    }

    private async Task<int> ChaseAsync(
        RequestApprovalInstance instance,
        RequestApprovalStep step,
        ApprovalWorkflowStage stage,
        ServiceRequest ticket,
        DateTime now,
        CancellationToken ct)
    {
        var activated = step.ActivatedOnUtc!.Value;
        var sent = 0;

        // Escalation first. A level that is far enough past its due time to
        // escalate does not also need a reminder in the same pass — the person
        // who is not answering has already had one, and telling them again in
        // the same minute somebody is escalated over their head is noise.
        var escalation = ApprovalSchedule.EscalationOccurrence(step, stage, now);

        if (escalation > 0)
        {
            await notifications.ChaseAsync(
                instance, step, ticket, ApprovalNotificationType.Escalation, escalation, ct);

            return 1;
        }

        var reminder = ApprovalSchedule.ReminderOccurrence(stage, activated, now);

        if (reminder > 0)
        {
            await notifications.ChaseAsync(
                instance, step, ticket, ApprovalNotificationType.Reminder, reminder, ct);

            sent++;
        }

        return sent;
    }
}
