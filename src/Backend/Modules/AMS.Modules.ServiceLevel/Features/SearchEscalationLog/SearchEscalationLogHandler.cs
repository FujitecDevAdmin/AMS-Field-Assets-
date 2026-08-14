using AMS.Modules.ServiceLevel.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.Features.SearchEscalationLog;

/// <summary>
/// Which escalations actually fired. Catalogue: the SLA panel on Request
/// Detail.
/// </summary>
/// <remarks>
/// The log is evidence, and this is how anybody reads it. "Nobody told me"
/// is answerable only if what was sent, to whom and when is recorded — and a
/// Failed row is as much of an answer as a Sent one.
/// </remarks>
public sealed class SearchEscalationLogHandler(ServiceLevelDbContext db)
    : IRequestHandler<SearchEscalationLogQuery, SearchEscalationLogResponse>
{
    public async Task<Result<SearchEscalationLogResponse>> HandleAsync(
        SearchEscalationLogQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.SlaEscalationLogs.AsNoTracking();

        if (request.ServiceRequestId is { } ticketId)
        {
            query = query.Where(l => l.ServiceRequestId == ticketId);
        }

        if (request.Outcome is { } outcome)
        {
            query = query.Where(l => l.Outcome == outcome);
        }

        var rows = await query
            .OrderByDescending(l => l.FiredOnUtc)
            .ThenByDescending(l => l.Id)
            .Take(request.Take)
            .Select(l => new SearchEscalationLogResponse.Row(
                l.Id, l.ServiceRequestId, l.SlaEscalationId, l.EscalationType, l.Level,
                l.SentTo, l.Channel, l.Outcome, l.FailureReason, l.FiredOnUtc))
            .ToListAsync(ct);

        return new SearchEscalationLogResponse(rows);
    }
}
