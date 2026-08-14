using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.CreateApprovalWorkflow;

/// <summary>
/// Draft a new approval route, or a new version of one. Catalogue: Approval Workflow Setup.
/// </summary>
public sealed record CreateApprovalWorkflowCommand(
    string WorkflowName,
    string? Description,
    int? ServiceTemplateId,
    int? LocationId,
    string? Priority,
    bool IsDefault,
    IReadOnlyList<CreateApprovalWorkflowCommand.Stage> Stages) : ICommand<CreateApprovalWorkflowResponse>
{
    /// <summary>One level of the route.</summary>
    /// <param name="StageNumber">Its place in the order, numbered from one by the mapper.</param>
    /// <param name="StageName">What the screen calls it: "Reporting manager", "IT head".</param>
    /// <param name="ApprovalMode">Any or All.</param>
    /// <param name="DueAfterMinutes">How long this level has, once it becomes its turn.</param>
    /// <param name="ReminderAfterMinutes">When to nudge.</param>
    /// <param name="ReminderRepeatMinutes">How often to keep nudging.</param>
    /// <param name="EscalateAfterMinutes">How long after it falls due before it goes up.</param>
    /// <param name="AllowDelegation">Whether an approver may hand it on.</param>
    /// <param name="Rules">How to find this level's approvers.</param>
    public sealed record Stage(
        int StageNumber,
        string StageName,
        string ApprovalMode,
        int? DueAfterMinutes,
        int? ReminderAfterMinutes,
        int? ReminderRepeatMinutes,
        int? EscalateAfterMinutes,
        bool AllowDelegation,
        IReadOnlyList<Rule> Rules);

    /// <summary>One way of finding approvers for a level.</summary>
    /// <param name="ResolverType">User, Role, Capability, EmployeeManager, RequesterManager, LocationBranchAdmin or CustomEmail.</param>
    /// <param name="ResolverUserId">Identity.User, id only. Required for User.</param>
    /// <param name="ResolverRoleId">Identity.Role, id only. Required for Role.</param>
    /// <param name="ResolverCapabilityName">Required for Capability and LocationBranchAdmin.</param>
    /// <param name="ResolverEmail">Required for CustomEmail — an approver with no login.</param>
    /// <param name="DisplayName">What to call them when the resolver cannot say.</param>
    /// <param name="IsRequired">Whether an All level must wait for this one.</param>
    public sealed record Rule(
        string ResolverType,
        int? ResolverUserId,
        int? ResolverRoleId,
        string? ResolverCapabilityName,
        string? ResolverEmail,
        string? DisplayName,
        bool IsRequired);
}
