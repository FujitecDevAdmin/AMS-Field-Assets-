using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.CreateApprovalWorkflow;

/// <summary>
/// Draft a route, or a new version of one. Catalogue: Approval Workflow Setup.
/// </summary>
/// <remarks>
/// <para>
/// A whole route arrives at once — stages and their approver rules together —
/// for the same reason a support team's membership does: it is one thing, and
/// an endpoint per stage would let a half-built route exist, be picked up by a
/// submission, and approve something on rules nobody finished writing.
/// </para>
/// <para>
/// A published definition is never edited. Send it again and it becomes the
/// next VersionNumber; the old one is retired through
/// <c>PublishApprovalWorkflow</c>. Editing in place would rewrite the rules an
/// in-flight approval is being judged by, which is the one thing an approval
/// audit must be able to rule out.
/// </para>
/// <para>
/// It is created as a DRAFT. Publishing is a separate, deliberate act.
/// </para>
/// </remarks>
public sealed class CreateApprovalWorkflowHandler(
    ServiceDeskDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<CreateApprovalWorkflowCommand, CreateApprovalWorkflowResponse>
{
    public async Task<Result<CreateApprovalWorkflowResponse>> HandleAsync(
        CreateApprovalWorkflowCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var invalid = await ValidateAsync(request, ct);
        if (invalid is not null)
        {
            return invalid;
        }

        var now = clock.UtcNow;

        // The next version of this name. UX_ApprovalWorkflowDefinition_NameVersion
        // catches the race if two administrators draft at once; this is what
        // makes the ordinary case give a sensible number.
        var version = await db.ApprovalWorkflowDefinitions
            .Where(w => w.WorkflowName == request.WorkflowName)
            .MaxAsync(w => (int?)w.VersionNumber, ct) ?? 0;

        var definition = new ApprovalWorkflowDefinition
        {
            WorkflowName = request.WorkflowName,
            VersionNumber = version + 1,
            Description = request.Description,
            ServiceTemplateId = request.ServiceTemplateId,
            LocationId = request.LocationId,
            Priority = request.Priority,
            // Not default yet, whatever was asked for: an unpublished default
            // would take the one live default slot
            // (UX_ApprovalWorkflowDefinition_OneActiveDefault) away from the
            // route currently doing the job. It is claimed at publication.
            IsDefault = false,
            IsPublished = false,
            IsActive = true,
            CreatedOnUtc = now,
            CreatedBy = currentUser.Username,
        };

        db.ApprovalWorkflowDefinitions.Add(definition);

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

        foreach (var stage in request.Stages)
        {
            var row = new ApprovalWorkflowStage
            {
                ApprovalWorkflowId = definition.Id,
                StageNumber = stage.StageNumber,
                StageName = stage.StageName,
                ApprovalMode = stage.ApprovalMode,
                DueAfterMinutes = stage.DueAfterMinutes,
                ReminderAfterMinutes = stage.ReminderAfterMinutes,
                ReminderRepeatMinutes = stage.ReminderRepeatMinutes,
                EscalateAfterMinutes = stage.EscalateAfterMinutes,
                AllowDelegation = stage.AllowDelegation,
                IsEnabled = true,
                CreatedOnUtc = now,
                CreatedBy = currentUser.Username,
            };

            db.ApprovalWorkflowStages.Add(row);
            await db.SaveChangesAsync(ct);

            foreach (var rule in stage.Rules)
            {
                db.ApprovalStageApproverRules.Add(new ApprovalStageApproverRule
                {
                    ApprovalWorkflowStageId = row.Id,
                    ResolverType = rule.ResolverType,
                    ResolverUserId = rule.ResolverUserId,
                    ResolverRoleId = rule.ResolverRoleId,
                    ResolverCapabilityName = rule.ResolverCapabilityName,
                    ResolverEmail = rule.ResolverEmail,
                    DisplayName = rule.DisplayName,
                    IsRequired = rule.IsRequired,
                    IsEnabled = true,
                    CreatedOnUtc = now,
                    CreatedBy = currentUser.Username,
                });
            }
        }

        await db.SaveChangesAsync(ct);

        return new CreateApprovalWorkflowResponse(
            definition.Id, definition.WorkflowName, definition.VersionNumber,
            request.Stages.Count);
    }

    /// <summary>
    /// Everything the CHECK constraints would reject, refused first and by
    /// name.
    /// </summary>
    /// <remarks>
    /// CK_ApprovalStageApproverRule_Value is the reason this matters more here
    /// than elsewhere: it is a seven-branch constraint, and the 500 it produces
    /// tells an administrator building a route absolutely nothing about which
    /// field they left empty.
    /// </remarks>
    private async Task<Error?> ValidateAsync(
        CreateApprovalWorkflowCommand request,
        CancellationToken ct)
    {
        if (request.Priority is { } priority
            && !RequestPriority.All.Contains(priority, StringComparer.Ordinal))
        {
            return Error.Validation(
                "ApprovalWorkflow.UnknownPriority",
                $"Priority must be one of {string.Join(", ", RequestPriority.All)}, or left empty for all.");
        }

        if (request.ServiceTemplateId is { } templateId
            && !await db.ServiceTemplates.AnyAsync(t => t.Id == templateId, ct))
        {
            return Error.NotFound("ServiceTemplate", templateId);
        }

        foreach (var stage in request.Stages)
        {
            if (!ApprovalMode.Allowed.Contains(stage.ApprovalMode, StringComparer.Ordinal))
            {
                return Error.Validation(
                    "ApprovalWorkflow.UnknownMode",
                    $"Stage {stage.StageNumber}: mode must be Any or All.");
            }

            if (stage.Rules.Count == 0)
            {
                // A level with no rules resolves to nobody, and a level waiting
                // on nobody never completes. The run would sit there for ever.
                return Error.Validation(
                    "ApprovalWorkflow.StageHasNoApprovers",
                    $"Stage {stage.StageNumber} has no approver rules.");
            }

            foreach (var rule in stage.Rules)
            {
                var error = ValidateRule(stage.StageNumber, rule);
                if (error is not null)
                {
                    return error;
                }
            }
        }

        return null;
    }

    private static Error? ValidateRule(int stageNumber, CreateApprovalWorkflowCommand.Rule rule)
    {
        if (!ResolverType.Allowed.Contains(rule.ResolverType, StringComparer.Ordinal))
        {
            return Error.Validation(
                "ApprovalWorkflow.UnknownResolver",
                $"Stage {stageNumber}: resolver must be one of {string.Join(", ", ResolverType.Allowed)}.");
        }

        var missing = rule.ResolverType switch
        {
            ResolverType.User when rule.ResolverUserId is null => "a user",
            ResolverType.Role when rule.ResolverRoleId is null => "a role",
            ResolverType.Capability when string.IsNullOrWhiteSpace(rule.ResolverCapabilityName)
                => "a capability name",
            ResolverType.LocationBranchAdmin when string.IsNullOrWhiteSpace(rule.ResolverCapabilityName)
                => "a capability name",
            ResolverType.CustomEmail when string.IsNullOrWhiteSpace(rule.ResolverEmail)
                => "an e-mail address",
            _ => null,
        };

        return missing is null
            ? null
            : Error.Validation(
                "ApprovalWorkflow.ResolverIncomplete",
                $"Stage {stageNumber}: a {rule.ResolverType} rule needs {missing}.");
    }
}
