namespace AMS.Modules.ServiceDesk.Features.CreateApprovalWorkflow;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateApprovalWorkflowRequest(
    string WorkflowName,
    string? Description,
    int? ServiceTemplateId,
    int? LocationId,
    string? Priority,
    bool? IsDefault,
    IReadOnlyList<CreateApprovalWorkflowRequest.Stage> Stages)
{
    /// <summary>One level of the route, as the setup screen sends it.</summary>
    /// <remarks>
    /// No StageNumber: the order is the order they arrive in, numbered by the
    /// mapper. A client that sent its own numbers could send 1, 2, 2.
    /// </remarks>
    public sealed record Stage(
        string StageName,
        string ApprovalMode,
        int? DueAfterMinutes,
        int? ReminderAfterMinutes,
        int? ReminderRepeatMinutes,
        int? EscalateAfterMinutes,
        bool? AllowDelegation,
        IReadOnlyList<Rule> Rules)
    {
        /// <summary>How to find this level's approvers. At least one is needed.</summary>
        public IReadOnlyList<Rule> Rules { get; init; } = Rules ?? [];
    }

    /// <summary>One way of finding approvers for a level.</summary>
    public sealed record Rule(
        string ResolverType,
        int? ResolverUserId,
        int? ResolverRoleId,
        string? ResolverCapabilityName,
        string? ResolverEmail,
        string? DisplayName,
        bool? IsRequired);
}
