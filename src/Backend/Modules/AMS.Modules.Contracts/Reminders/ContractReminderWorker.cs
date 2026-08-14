using AMS.Modules.Contracts.Domain;
using AMS.Modules.Contracts.Persistence;
using AMS.Modules.Notifications.PublicApi.Notifications;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.SharedKernel.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Contracts.Reminders;

/// <summary>
/// Tells somebody a contract is about to run out.
/// </summary>
/// <remarks>
/// <para>
/// A daily pass. The design script says what makes it safe to run more often
/// than that: "the daily job is idempotent because of
/// <c>UX_ContractReminderLog_OncePerThreshold</c>, not because it remembers
/// having run". Nothing here tracks what it did yesterday; it asks the log.
/// </para>
/// <para>
/// R2-2 is the subtle part. The log's unique key includes the expiry date the
/// reminder was measured against, so a renewed contract — same row, new end
/// date — earns its whole ladder again for the NEW expiry rather than being
/// permanently silent because it was reminded about last year's.
/// </para>
/// </remarks>
public sealed class ContractReminderWorker(
    ContractsDbContext db,
    INotifier notifier,
    IVendorDirectory vendors,
    IClock clock)
{
    /// <summary>
    /// Sends everything due today. Returns how many reminders went out.
    /// </summary>
    /// <remarks>
    /// Callable directly, so it can be tested by moving a clock rather than by
    /// waiting a day. A worker that only runs on a timer is a worker nobody can
    /// test.
    /// </remarks>
    public async Task<int> RunAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow);

        // The furthest window anybody has configured. Reading every live
        // contract to find the handful that are close would be a table scan a
        // day for an answer IX_Contract_EndDate can give.
        var horizon = await db.ContractReminderSettings
            .AsNoTracking()
            .Where(s => s.IsActive)
            .MaxAsync(s => (int?)s.DaysBeforeExpiry, ct) ?? 0;

        if (horizon == 0)
        {
            return 0;
        }

        var due = await db.Contracts
            .AsNoTracking()
            .Where(c => !c.IsDeleted
                && c.EndDate >= today
                && c.EndDate <= today.AddDays(horizon))
            .ToListAsync(ct);

        if (due.Count == 0)
        {
            return 0;
        }

        var windows = await ReminderWindows.ResolveManyAsync(
            db, due.ConvertAll(c => c.Id), ct);

        var sent = 0;

        foreach (var contract in due)
        {
            var daysLeft = contract.EndDate.DayNumber - today.DayNumber;

            foreach (var window in windows[contract.Id])
            {
                // Fires on the day the window opens and every day after, until
                // the log row stops it. That way a pass that did not run
                // yesterday still sends today's reminder rather than missing it
                // for ever - which a strict equality check would do.
                if (daysLeft > window.DaysBeforeExpiry)
                {
                    continue;
                }

                if (await AlreadySentAsync(contract.Id, window.DaysBeforeExpiry, contract.EndDate, ct))
                {
                    continue;
                }

                await SendAsync(contract, window, today, ct);
                sent++;
            }
        }

        if (sent > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return sent;
    }

    /// <summary>
    /// Whether this window has already gone out for this expiry date.
    /// </summary>
    /// <remarks>
    /// A Failed row does not count. R2-3 excludes them from the unique index
    /// deliberately, so a send that failed to queue can be retried tomorrow
    /// instead of being blocked for ever.
    /// </remarks>
    private async Task<bool> AlreadySentAsync(
        int contractId,
        int daysBeforeExpiry,
        DateOnly expiry,
        CancellationToken ct) =>
        await db.ContractReminderLogs.AnyAsync(
            l => l.ContractId == contractId
                && l.DaysBeforeExpiry == daysBeforeExpiry
                && l.ExpiryDateSnapshot == expiry
                && l.Outcome != ReminderOutcome.Failed, ct);

    private async Task SendAsync(
        Contract contract,
        ReminderWindow window,
        DateOnly today,
        CancellationToken ct)
    {
        var recipients = await RecipientsAsync(contract, window, ct);

        var daysLeft = contract.EndDate.DayNumber - today.DayNumber;

        var subject = daysLeft == 0
            ? $"Contract expires today: {contract.ContractNumber} — {contract.ContractName}"
            : $"Contract expires in {daysLeft} days: {contract.ContractNumber} — "
                + contract.ContractName;

        var body = string.Join(
            Environment.NewLine,
            daysLeft == 0
                ? "This contract expires today."
                : $"This contract expires in {daysLeft} days.",
            string.Empty,
            $"Number: {contract.ContractNumber}",
            $"Name:   {contract.ContractName}",
            $"Type:   {contract.ContractType}",
            $"Expiry: {contract.EndDate:yyyy-MM-dd}",
            contract.AutoRenew
                ? "It is marked to renew automatically."
                : "It is NOT marked to renew automatically.");

        long? outboxId = null;
        var wantsEmail = window.Channel is ReminderChannel.Email or ReminderChannel.Both;

        if (wantsEmail && recipients.Count > 0)
        {
            outboxId = await notifier.QueueEmailAsync(
                new OutboundEmail(
                    string.Join(';', recipients),
                    null,
                    subject,
                    body,
                    IsHtml: false,
                    EmailSource.Contract,
                    contract.Id),
                ct);
        }

        db.ContractReminderLogs.Add(new ContractReminderLog
        {
            ContractId = contract.Id,
            DaysBeforeExpiry = window.DaysBeforeExpiry,
            // R2-2: the expiry this was measured against, so a renewal earns
            // the ladder again.
            ExpiryDateSnapshot = contract.EndDate,
            SentOnDate = today,
            SentTo = recipients.Count > 0 ? string.Join(';', recipients) : null,
            EmailOutboxId = outboxId,
            // A row with no recipients is still a row. It says the window fired
            // and reached nobody, which is a configuration problem somebody has
            // to see — and it stops the worker rediscovering the same silent
            // window every day.
            Outcome = outboxId is null ? ReminderOutcome.Sent : ReminderOutcome.Queued,
        });
    }

    /// <summary>
    /// Who a reminder goes to: whoever the window names, or the vendor.
    /// </summary>
    /// <remarks>
    /// The vendor is the useful default because the person who can do something
    /// about an expiring AMC usually works for them. A contract with no vendor
    /// and no named recipients reaches nobody, and the log row says so.
    /// </remarks>
    private async Task<IReadOnlyList<string>> RecipientsAsync(
        Contract contract,
        ReminderWindow window,
        CancellationToken ct)
    {
        if (window.Recipients is { Length: > 0 } named)
        {
            return [.. named
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries
                    | StringSplitOptions.TrimEntries)];
        }

        if (contract.VendorId is not { } vendorId)
        {
            return [];
        }

        var vendor = await vendors.FindAsync(vendorId, ct);

        return vendor?.Email is { Length: > 0 } email ? [email] : [];
    }
}
