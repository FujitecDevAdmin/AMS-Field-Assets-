using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.SearchApprovalWorkflows;

/// <summary>
/// The routes and their versions. Catalogue: Approval Workflow Setup.
/// </summary>
/// <remarks>
/// Every version, not just the live one. Reading an approval that ran last
/// March means reading the rules as they were last March, and a screen that
/// only shows the current version cannot explain a decision anybody is
/// querying.
/// </remarks>
public sealed class SearchApprovalWorkflowsHandler(ServiceDeskDbContext db)
    : IRequestHandler<SearchApprovalWorkflowsQuery, SearchApprovalWorkflowsResponse>
{
    public async Task<Result<SearchApprovalWorkflowsResponse>> HandleAsync(
        SearchApprovalWorkflowsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.ApprovalWorkflowDefinitions.AsNoTracking();

        if (request.Name is { } name)
        {
            query = query.Where(w => w.WorkflowName.Contains(name));
        }

        if (request.PublishedOnly)
        {
            query = query.Where(w => w.IsPublished);
        }

        if (request.ActiveOnly)
        {
            query = query.Where(w => w.IsActive);
        }

        if (request.ServiceTemplateId is { } templateId)
        {
            query = query.Where(w => w.ServiceTemplateId == templateId);
        }

        var definitions = await query
            .OrderBy(w => w.WorkflowName)
            .ThenByDescending(w => w.VersionNumber)
            .ToListAsync(ct);

        var ids = definitions.ConvertAll(w => w.Id);

        var stages = await db.ApprovalWorkflowStages
            .AsNoTracking()
            .Where(s => ids.Contains(s.ApprovalWorkflowId))
            .OrderBy(s => s.StageNumber)
            .ToListAsync(ct);

        var stageIds = stages.ConvertAll(s => s.Id);

        var rules = await db.ApprovalStageApproverRules
            .AsNoTracking()
            .Where(r => stageIds.Contains(r.ApprovalWorkflowStageId))
            .OrderBy(r => r.Id)
            .ToListAsync(ct);

        // Three reads and a join in memory. A route has a handful of stages and
        // each a handful of rules; loading them as one flattened join would
        // repeat the definition on every rule row and cost more to take apart
        // than to fetch.
        var rulesByStage = rules
            .GroupBy(r => r.ApprovalWorkflowStageId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var stagesByWorkflow = stages
            .GroupBy(s => s.ApprovalWorkflowId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = definitions.ConvertAll(w => new SearchApprovalWorkflowsResponse.Row(
            w.Id,
            w.WorkflowName,
            w.VersionNumber,
            w.Description,
            w.ServiceTemplateId,
            w.LocationId,
            w.Priority,
            w.IsDefault,
            w.IsPublished,
            w.IsActive,
            w.EffectiveFromUtc,
            w.EffectiveToUtc,
            stagesByWorkflow.TryGetValue(w.Id, out var own)
                ? own.ConvertAll(s => new SearchApprovalWorkflowsResponse.Stage(
                    s.Id,
                    s.StageNumber,
                    s.StageName,
                    s.ApprovalMode,
                    s.DueAfterMinutes,
                    s.EscalateAfterMinutes,
                    s.AllowDelegation,
                    s.IsEnabled,
                    rulesByStage.TryGetValue(s.Id, out var stageRules)
                        ? stageRules.ConvertAll(r => new SearchApprovalWorkflowsResponse.Rule(
                            r.Id, r.ResolverType, r.ResolverUserId, r.ResolverRoleId,
                            r.ResolverCapabilityName, r.ResolverEmail, r.DisplayName,
                            r.IsRequired, r.IsEnabled))
                        : []))
                : []));

        return new SearchApprovalWorkflowsResponse(rows);
    }
}
