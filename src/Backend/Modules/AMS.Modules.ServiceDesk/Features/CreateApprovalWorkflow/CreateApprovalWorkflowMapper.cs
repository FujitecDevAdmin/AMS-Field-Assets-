namespace AMS.Modules.ServiceDesk.Features.CreateApprovalWorkflow;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateApprovalWorkflowMapper
{
    public static CreateApprovalWorkflowCommand ToCommand(CreateApprovalWorkflowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateApprovalWorkflowCommand(
            request.WorkflowName.Trim(),
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            request.ServiceTemplateId,
            request.LocationId,
            string.IsNullOrWhiteSpace(request.Priority) ? null : request.Priority.Trim(),
            request.IsDefault ?? false,
            [.. request.Stages.Select((s, index) => new CreateApprovalWorkflowCommand.Stage(
                index + 1,
                s.StageName.Trim(),
                s.ApprovalMode.Trim(),
                s.DueAfterMinutes,
                s.ReminderAfterMinutes,
                s.ReminderRepeatMinutes,
                s.EscalateAfterMinutes,
                s.AllowDelegation ?? false,
                [.. s.Rules.Select(r => new CreateApprovalWorkflowCommand.Rule(
                    r.ResolverType.Trim(),
                    r.ResolverUserId,
                    r.ResolverRoleId,
                    string.IsNullOrWhiteSpace(r.ResolverCapabilityName) ? null : r.ResolverCapabilityName.Trim(),
                    string.IsNullOrWhiteSpace(r.ResolverEmail) ? null : r.ResolverEmail.Trim(),
                    string.IsNullOrWhiteSpace(r.DisplayName) ? null : r.DisplayName.Trim(),
                    r.IsRequired ?? true))]))]);
    }
}
