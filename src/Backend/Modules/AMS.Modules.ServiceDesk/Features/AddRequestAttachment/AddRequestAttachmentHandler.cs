using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.AddRequestAttachment;

/// <summary>
/// Record a file against a ticket. Catalogue: Attachments.
/// </summary>
/// <remarks>
/// The row holds where the file is, not the file. Storing bytes in the
/// database would put a screenshot of an error message in every backup of the
/// ticket table forever.
/// </remarks>
public sealed class AddRequestAttachmentHandler(
    ServiceDeskDbContext db,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<AddRequestAttachmentCommand, AddRequestAttachmentResponse>
{
    public async Task<Result<AddRequestAttachmentResponse>> HandleAsync(
        AddRequestAttachmentCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!AttachmentKind.All.Contains(request.AttachmentType, StringComparer.Ordinal))
        {
            return Error.Validation(
                "RequestAttachment.UnknownType",
                $"Attachment type must be one of {string.Join(", ", AttachmentKind.All)}.");
        }

        var ticket = await db.ServiceRequests.SingleOrDefaultAsync(r => r.Id == request.Id, ct);
        if (ticket is null)
        {
            return Error.NotFound("ServiceRequest", request.Id);
        }

        var status = await db.RequestStatuses.SingleAsync(s => s.Id == ticket.RequestStatusId, ct);

        var closed = TicketGuards.RefuseIfClosed(status, "attaching a file");
        if (closed is not null)
        {
            return closed;
        }

        var attachment = new RequestAttachment
        {
            ServiceRequestId = ticket.Id,
            AttachmentType = request.AttachmentType,
            FilePath = request.FilePath,
            FileName = request.FileName,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            UploadedByUserId = currentUser.Id,
            UploadedOnUtc = clock.UtcNow,
        };

        db.RequestAttachments.Add(attachment);

        await db.SaveChangesAsync(ct);

        return new AddRequestAttachmentResponse(
            attachment.Id, ticket.Id, attachment.AttachmentType, attachment.FileName);
    }
}
