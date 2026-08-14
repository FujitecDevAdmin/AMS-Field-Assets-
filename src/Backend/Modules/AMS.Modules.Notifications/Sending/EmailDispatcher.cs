using AMS.Modules.Notifications.Domain;
using AMS.Modules.Notifications.Persistence;
using AMS.SharedKernel.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMS.Modules.Notifications.Sending;

/// <summary>How hard the dispatcher tries, and how often.</summary>
/// <param name="PollSeconds">How long to wait when there was nothing to send.</param>
/// <param name="BatchSize">How many to take in one pass.</param>
/// <param name="MaxAttempts">
/// How many times a message is tried before it is given up on. A message that
/// is retried for ever is a message nobody ever looks at, and a Failed row on
/// a screen is the only thing that gets a wrong address corrected.
/// </param>
public sealed record DispatcherOptions(int PollSeconds = 15, int BatchSize = 20, int MaxAttempts = 5);

/// <summary>
/// Takes messages out of the outbox and sends them.
/// </summary>
/// <remarks>
/// <para>
/// The other half of the promise the outbox makes. Queuing without something
/// that drains the queue is a table that grows, and every module in this system
/// now queues: ticket replies, approval requests, SLA escalations, contract
/// reminders.
/// </para>
/// <para>
/// It is deliberately dull. One profile, oldest first, one at a time, count the
/// attempts, give up after enough of them. What it must never do is lose a
/// message or send one twice, and both of those come from the same discipline:
/// the row is updated in the same pass that sent it, and a row is only ever
/// taken when it is Pending.
/// </para>
/// </remarks>
public sealed class EmailDispatcher(
    NotificationsDbContext db,
    IEmailTransport transport,
    SmtpPasswordProtector protector,
    IClock clock,
    ILogger<EmailDispatcher> logger,
    DispatcherOptions options)
{
    /// <summary>
    /// Sends one batch. Returns how many were attempted.
    /// </summary>
    /// <remarks>
    /// Separate from any timer loop so it can be called directly — by a test,
    /// by an administrator pressing "send now", or by a hosted service. A
    /// background worker that is the only way to run the thing is a worker
    /// nobody can test.
    /// </remarks>
    public async Task<int> SendBatchAsync(CancellationToken ct)
    {
        var profile = await ActiveProfileAsync(ct);

        if (profile is null)
        {
            // Nothing is marked Failed. A site that has not configured SMTP yet
            // has a queue that will send the moment it does, and burning the
            // attempt counter in the meantime would exhaust it before the first
            // real try.
            DispatcherLog.NoProfile(logger);

            return 0;
        }

        var pending = await db.EmailOutboxes
            .Where(m => m.Status == OutboxStatus.Pending)
            .OrderBy(m => m.CreatedOnUtc)
            .ThenBy(m => m.Id)
            .Take(options.BatchSize)
            .ToListAsync(ct);

        foreach (var message in pending)
        {
            await AttemptAsync(profile, message, ct);
        }

        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return pending.Count;
    }

    private async Task AttemptAsync(EmailProfile profile, EmailOutbox message, CancellationToken ct)
    {
        message.AttemptCount++;

        try
        {
            await transport.SendAsync(
                profile,
                new OutgoingMessage(
                    message.ToAddress, message.CcAddress, message.Subject,
                    message.Body, message.IsHtml),
                ct);

            message.Status = OutboxStatus.Sent;
            message.SentOnUtc = clock.UtcNow;
            message.LastError = null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Every failure is the same failure as far as this is concerned: a
            // refused address and a host that is down both mean "not sent", and
            // guessing which from an exception type is how a transient outage
            // becomes a permanent one.
            message.LastError = Truncate(ex.Message);

            if (message.AttemptCount >= options.MaxAttempts)
            {
                message.Status = OutboxStatus.Failed;

                DispatcherLog.GaveUp(
                    logger, ex, message.Id, message.ToAddress, message.AttemptCount);
            }
            else
            {
                DispatcherLog.AttemptFailed(logger, ex, message.AttemptCount, message.Id);
            }
        }
    }

    /// <summary>
    /// The profile to send through: the default one, or the only active one.
    /// </summary>
    /// <remarks>
    /// <c>UX_EmailSetting_OneDefault</c> allows one live default, so this cannot
    /// pick the wrong of two. A site with exactly one active profile and nobody
    /// having ticked "default" still sends, because refusing to would be
    /// pedantry about a choice with one option.
    /// </remarks>
    private async Task<EmailProfile?> ActiveProfileAsync(CancellationToken ct)
    {
        var settings = await db.EmailSettings
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.IsDefault)
            .ThenBy(s => s.Id)
            .FirstOrDefaultAsync(ct);

        if (settings is null)
        {
            return null;
        }

        string? password = null;

        if (settings.SmtpPasswordEncrypted is { Length: > 0 } encrypted)
        {
            try
            {
                password = protector.Unprotect(encrypted);
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                // The key ring has moved or been rotated. Say so plainly: the
                // alternative is every message failing with an authentication
                // error that points at the mail server rather than at us.
                DispatcherLog.PasswordUnreadable(logger, ex, settings.ProfileName);

                return null;
            }
        }

        return new EmailProfile(
            settings.Host, settings.Port, settings.UseSsl, settings.FromAddress,
            settings.Username, password);
    }

    private static string Truncate(string message) =>
        message.Length <= 500 ? message : message[..497] + "...";
}
