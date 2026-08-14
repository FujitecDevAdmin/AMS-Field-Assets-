using AMS.Modules.ServiceLevel.Domain;
using AMS.Modules.ServiceLevel.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.Features.SearchSlaPolicies;

/// <summary>The policies and their ladders. Catalogue: SLA Policy Setup.</summary>
/// <remarks>
/// Ordered by urgency rather than alphabetically. The screen is a table of four
/// rows that a reader compares top to bottom, and Critical above Low is the
/// comparison they are making.
/// </remarks>
public sealed class SearchSlaPoliciesHandler(ServiceLevelDbContext db)
    : IRequestHandler<SearchSlaPoliciesQuery, SearchSlaPoliciesResponse>
{
    public async Task<Result<SearchSlaPoliciesResponse>> HandleAsync(
        SearchSlaPoliciesQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.SlaPolicies.AsNoTracking();

        if (request.ActiveOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        if (request.Priority is { } priority)
        {
            query = query.Where(p => p.Priority == priority);
        }

        var policies = await query.ToListAsync(ct);

        var ids = policies.ConvertAll(p => p.Id);

        var escalations = await db.SlaEscalations
            .AsNoTracking()
            .Where(e => ids.Contains(e.SlaPolicyId))
            .OrderBy(e => e.EscalationType)
            .ThenBy(e => e.Level)
            .ToListAsync(ct);

        var byPolicy = escalations
            .GroupBy(e => e.SlaPolicyId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = policies
            .OrderBy(p => Urgency(p.Priority))
            .ThenBy(p => p.PolicyName, StringComparer.Ordinal)
            .Select(p => new SearchSlaPoliciesResponse.Row(
                p.Id,
                p.PolicyName,
                p.Description,
                p.Priority,
                p.ResponseTargetMinutes,
                p.ResolutionTargetMinutes,
                p.RespectOperationalHours,
                p.RespectHolidays,
                p.RespectWeekends,
                p.NearDueWarningMinutes,
                p.IsActive,
                byPolicy.TryGetValue(p.Id, out var own)
                    ? own.ConvertAll(e => new SearchSlaPoliciesResponse.Escalation(
                        e.Id, e.EscalationType, e.Level, e.ThresholdPercent,
                        e.RecipientType, e.RecipientAddress, e.Channel, e.IsEnabled))
                    : []))
            .ToList();

        return new SearchSlaPoliciesResponse(rows);
    }

    private static int Urgency(string priority) => priority switch
    {
        SlaPriority.Critical => 0,
        SlaPriority.High => 1,
        SlaPriority.Medium => 2,
        _ => 3,
    };
}
