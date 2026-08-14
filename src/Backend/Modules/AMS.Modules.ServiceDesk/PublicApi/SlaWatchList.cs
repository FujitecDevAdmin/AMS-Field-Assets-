using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.Modules.ServiceDesk.PublicApi.ServiceDesk;
using AMS.SharedKernel.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.PublicApi;

/// <summary>ServiceDesk's answer to "what might be late".</summary>
/// <remarks>
/// The read is deliberately generous and the write deliberately narrow. Letting
/// the monitor ask for "tickets past their due date" would put the rule about
/// what late MEANS in two modules; letting it write anything but a timeline
/// entry would let a notification job change a ticket.
/// </remarks>
public sealed class SlaWatchList(ServiceDeskDbContext db, IClock clock) : ISlaWatchList
{
    public async Task<IReadOnlyList<SlaWatchTicket>> OpenTicketsAsync(CancellationToken ct) =>
        await (
            from r in db.ServiceRequests.AsNoTracking()
            join s in db.RequestStatuses on r.RequestStatusId equals s.Id
            where !s.IsClosedState
                && r.SlaPolicyId != null
                && (r.ResponseDueOnUtc != null || r.ResolutionDueOnUtc != null)
            select new SlaWatchTicket(
                r.Id,
                r.RequestNumber,
                r.Subject,
                r.Priority,
                s.StatusName,
                r.SlaPolicyId!.Value,
                r.LocationId,
                r.ResponseDueOnUtc,
                r.ResolutionDueOnUtc,
                r.FirstResponseOnUtc,
                r.IsSlaPaused,
                r.AssignedToUserId,
                r.AssignedTeamId,
                r.RequestedByEmployeeId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<int>> TeamLeadsAsync(int supportTeamId, CancellationToken ct) =>
        await db.SupportTeamMembers
            .AsNoTracking()
            .Where(m => m.SupportTeamId == supportTeamId && m.IsLead)
            .Select(m => m.UserId)
            .ToListAsync(ct);

    public async Task NoteEscalationAsync(int ticketId, string text, CancellationToken ct)
    {
        db.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = ticketId,
            EntryKind = HistoryEntryKind.Escalation,
            EntryText = text.Length <= 500 ? text : string.Concat(text.AsSpan(0, 497), "..."),
            OccurredOnUtc = clock.UtcNow,
            // The design script's own convention for entries nobody made. A
            // background pass has no signed-in user, and attributing an
            // automatic escalation to whoever last touched the ticket would be
            // a lie the timeline cannot be read past.
            PerformedBy = "SLA Automation",
        });

        await db.SaveChangesAsync(ct);
    }
}
