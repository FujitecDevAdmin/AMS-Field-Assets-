using AMS.Modules.Notifications.PublicApi.Notifications;
using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.SendRequestEmail;

/// <summary>
/// Send e-mail from a ticket. Catalogue: Send e-mail on Request Detail.
/// </summary>
/// <remarks>
/// <para>
/// This slice writes the message down and hands it to the Notifications
/// module's outbox, which is the only thing in this system that talks to a
/// mail server. A dead SMTP host retries instead of losing what somebody
/// wrote.
/// </para>
/// <para>
/// The row this module keeps is the ticket's copy of the conversation; the
/// outbox row is the delivery attempt, and <c>EmailOutboxId</c> joins them. Two
/// rows rather than one because they answer different questions — "what was
/// said on this ticket" and "what did we manage to send" — and a ticket whose
/// history disappeared when a queue was tidied would be the wrong trade.
/// </para>
/// <para>
/// Status is Queued and not Sent, because sending has not happened yet — and
/// even when it has, SMTP acceptance is not inbox placement. The column records
/// what we know rather than what we hope.
/// </para>
/// </remarks>
public sealed class SendRequestEmailHandler(
    ServiceDeskDbContext db,
    INotifier notifier,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<SendRequestEmailCommand, SendRequestEmailResponse>
{
    public async Task<Result<SendRequestEmailResponse>> HandleAsync(
        SendRequestEmailCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ticket = await db.ServiceRequests.SingleOrDefaultAsync(r => r.Id == request.Id, ct);
        if (ticket is null)
        {
            return Error.NotFound("ServiceRequest", request.Id);
        }

        var status = await db.RequestStatuses.SingleAsync(s => s.Id == ticket.RequestStatusId, ct);

        var closed = TicketGuards.RefuseIfClosed(status, "sending e-mail from it");
        if (closed is not null)
        {
            return closed;
        }

        var now = clock.UtcNow;

        var message = new RequestEmail
        {
            ServiceRequestId = ticket.Id,
            Direction = EmailDirection.Outbound,
            ToAddresses = request.ToAddresses,
            CcAddresses = request.CcAddresses,
            Subject = request.Subject,
            Body = request.Body,
            IsHtml = request.IsHtml,
            Status = EmailStatus.Queued,
            // CK_RequestEmail_SentBy requires this for an Outbound message, and
            // it is the honest answer anyway: somebody pressed send.
            SentByUserId = currentUser.Id,
            QueuedOnUtc = now,
        };

        db.RequestEmails.Add(message);

        // Saved before the history entry because RequestHistory.RequestEmailId
        // is a real foreign key (R2-6) and there is no navigation property to
        // let EF work the order out — the modules hold ids, not object graphs.
        // Both saves are inside the command's one transaction, so a failure
        // here leaves neither row.
        await db.SaveChangesAsync(ct);

        // Queued inside the same transaction (rule 4a). A ticket that failed to
        // save having already e-mailed the requester about it is exactly what
        // an outbox is for, and it only holds if the insert rolls back with
        // everything else.
        message.EmailOutboxId = await notifier.QueueEmailAsync(
            new OutboundEmail(
                request.ToAddresses,
                request.CcAddresses,
                request.Subject,
                request.Body,
                request.IsHtml,
                EmailSource.ServiceRequest,
                ticket.Id),
            ct);

        TicketGuards.StampFirstResponse(ticket, now);
        ticket.ModifiedOnUtc = now;
        ticket.ModifiedBy = currentUser.Username;

        db.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = ticket.Id,
            EntryKind = HistoryEntryKind.Email,
            EntryText = $"E-mail to {request.ToAddresses}: {request.Subject}",
            Body = request.Body,
            RequestEmailId = message.Id,
            OccurredOnUtc = now,
            PerformedBy = currentUser.Username,
        });

        await db.SaveChangesAsync(ct);

        return new SendRequestEmailResponse(message.Id, ticket.Id, message.Status);
    }
}
