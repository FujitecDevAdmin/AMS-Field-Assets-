using AMS.Modules.Notifications.Domain;
using AMS.Modules.Notifications.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Notifications.Features.RequeueEmail;

/// <summary>Try a failed message again. Catalogue: the outbox queue.</summary>
/// <remarks>
/// The attempt count goes back to zero, not up. Somebody requeues a message
/// because they have fixed what stopped it — a wrong address, a mail server
/// that was down — and giving it one last try before it fails again would make
/// the button useless in exactly the case it exists for.
/// </remarks>
public sealed class RequeueEmailHandler(NotificationsDbContext db, IClock clock)
    : IRequestHandler<RequeueEmailCommand, RequeueEmailResponse>
{
    public async Task<Result<RequeueEmailResponse>> HandleAsync(
        RequeueEmailCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var message = await db.EmailOutboxes.SingleOrDefaultAsync(m => m.Id == request.Id, ct);
        if (message is null)
        {
            return Error.NotFound("EmailOutbox", request.Id);
        }

        if (message.Status == OutboxStatus.Sent)
        {
            // Requeuing a sent message would send it twice, and the person who
            // pressed the button would have no way of knowing they had.
            return Error.Conflict(
                "EmailOutbox.AlreadySent",
                "That message was sent. Queue a new one rather than sending it again.");
        }

        if (message.Status == OutboxStatus.Pending)
        {
            return Error.Conflict(
                "EmailOutbox.AlreadyQueued",
                "That message is already waiting to be sent.");
        }

        message.Status = OutboxStatus.Pending;
        message.AttemptCount = 0;
        message.LastError = null;
        message.SentOnUtc = null;
        message.CreatedOnUtc = clock.UtcNow;

        await db.SaveChangesAsync(ct);

        return new RequeueEmailResponse(message.Id, message.Status, message.AttemptCount);
    }
}
