using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.AddRequestNote;

/// <summary>
/// Add a note. Catalogue: Conversations and History.
/// </summary>
/// <remarks>
/// Notes are history entries, not a second table. One chronological list is
/// what the screen draws and what anybody reading the ticket afterwards needs:
/// a note that says "spoke to the user" makes sense next to the status change
/// it explains and nowhere else.
/// </remarks>
public sealed class AddRequestNoteHandler(
    ServiceDeskDbContext db,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<AddRequestNoteCommand, AddRequestNoteResponse>
{
    public async Task<Result<AddRequestNoteResponse>> HandleAsync(
        AddRequestNoteCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ticket = await db.ServiceRequests.SingleOrDefaultAsync(r => r.Id == request.Id, ct);
        if (ticket is null)
        {
            return Error.NotFound("ServiceRequest", request.Id);
        }

        var status = await db.RequestStatuses.SingleAsync(s => s.Id == ticket.RequestStatusId, ct);

        var closed = TicketGuards.RefuseIfClosed(status, "adding a note");
        if (closed is not null)
        {
            return closed;
        }

        var now = clock.UtcNow;

        // Only a public note answers "did anybody get back to them". An
        // internal note is the technician talking to the technician.
        if (!request.IsInternal)
        {
            TicketGuards.StampFirstResponse(ticket, now);
            ticket.ModifiedOnUtc = now;
            ticket.ModifiedBy = currentUser.Username;
        }

        var entry = new RequestHistory
        {
            ServiceRequestId = ticket.Id,
            EntryKind = HistoryEntryKind.Note,
            // The timeline shows a line; the note itself lives in Body, which
            // is nvarchar(max) and does not have to be cut to fit a summary.
            EntryText = Summarise(request.Note),
            Body = request.Note,
            IsInternal = request.IsInternal,
            OccurredOnUtc = now,
            PerformedBy = currentUser.Username,
        };

        db.RequestHistories.Add(entry);

        await db.SaveChangesAsync(ct);

        return new AddRequestNoteResponse(
            entry.Id, ticket.Id, entry.IsInternal, entry.OccurredOnUtc);
    }

    /// <summary>The first line, short enough for EntryText's 500 characters.</summary>
    private static string Summarise(string note)
    {
        var text = note.ReplaceLineEndings(" ").Trim();

        return text.Length <= 200 ? text : string.Concat(text.AsSpan(0, 197), "...");
    }
}
