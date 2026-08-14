using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.PublishApprovalWorkflow;

/// <summary>
/// Publish a draft route, or retire a published one. Catalogue: Approval
/// Workflow Setup.
/// </summary>
/// <remarks>
/// Publication is where a definition stops being a draft somebody is editing
/// and starts being something submissions pick up. It is also where a route
/// claims the single live-default slot, which is a filtered unique index
/// (R2-13) rather than a rule in code — two active defaults meant the
/// submission path picked whichever sorted first, the exact failure this
/// design exists to prevent.
/// </remarks>
public sealed class PublishApprovalWorkflowHandler(
    ServiceDeskDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<PublishApprovalWorkflowCommand, PublishApprovalWorkflowResponse>
{
    public async Task<Result<PublishApprovalWorkflowResponse>> HandleAsync(
        PublishApprovalWorkflowCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var definition = await db.ApprovalWorkflowDefinitions
            .SingleOrDefaultAsync(w => w.Id == request.Id, ct);

        if (definition is null)
        {
            return Error.NotFound("ApprovalWorkflow", request.Id);
        }

        if (request.EffectiveToUtc is { } to
            && request.EffectiveFromUtc is { } from
            && to <= from)
        {
            return Error.Validation(
                "ApprovalWorkflow.EffectiveRange",
                "The effective end must come after the start.");
        }

        if (request.IsPublished && !await HasStagesAsync(definition.Id, ct))
        {
            return Error.Validation(
                "ApprovalWorkflow.NoStages",
                "A route with no levels approves nothing. Add at least one before publishing.");
        }

        // Retiring a route that something is still waiting on would leave that
        // run pointing at a definition nobody can read on the setup screen.
        // The run keeps going either way - its steps are snapshots - but the
        // administrator should know before, not afterwards.
        if (!request.IsActive && await HasPendingRunsAsync(definition.Id, ct))
        {
            return Error.Conflict(
                "ApprovalWorkflow.InFlight",
                "Approvals are still running on this version. Wait for them, or cancel them first.");
        }

        var now = clock.UtcNow;

        definition.IsPublished = request.IsPublished;
        definition.IsActive = request.IsActive;
        definition.EffectiveFromUtc = request.EffectiveFromUtc ?? definition.EffectiveFromUtc;
        definition.EffectiveToUtc = request.EffectiveToUtc ?? definition.EffectiveToUtc;
        definition.ModifiedOnUtc = now;
        definition.ModifiedBy = currentUser.Username;

        // A retired route is nobody's default. Leaving the flag set would hold
        // the one live-default slot against a route that is no longer in use.
        if (!request.IsActive)
        {
            definition.IsDefault = false;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        return new PublishApprovalWorkflowResponse(
            definition.Id, definition.WorkflowName, definition.VersionNumber,
            definition.IsPublished, definition.IsActive);
    }

    private async Task<bool> HasStagesAsync(int workflowId, CancellationToken ct) =>
        await db.ApprovalWorkflowStages.AnyAsync(
            s => s.ApprovalWorkflowId == workflowId && s.IsEnabled, ct);

    private async Task<bool> HasPendingRunsAsync(int workflowId, CancellationToken ct) =>
        await db.RequestApprovalInstances.AnyAsync(
            i => i.ApprovalWorkflowId == workflowId
                && i.Status == ApprovalInstanceStatus.Pending, ct);
}
