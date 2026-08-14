namespace AMS.Modules.ServiceDesk.Features.SearchApprovalWorkflows;

/// <summary>
/// Every version of every route, newest version first.
/// </summary>
/// <param name="Rows">The routes, each with its stages and their approver rules.</param>
public sealed record SearchApprovalWorkflowsResponse(
    IReadOnlyList<SearchApprovalWorkflowsResponse.Row> Rows)
{
    /// <summary>One version of one route.</summary>
    /// <param name="Id">The definition.</param>
    /// <param name="WorkflowName">The route's name, shared by every version.</param>
    /// <param name="VersionNumber">Which version.</param>
    /// <param name="Description">What it is for.</param>
    /// <param name="ServiceTemplateId">The template it belongs to, if it is tied to one.</param>
    /// <param name="LocationId">The branch it applies at. Null means everywhere.</param>
    /// <param name="Priority">The priority it applies to. Null means all four.</param>
    /// <param name="IsDefault">Whether submissions fall back to it. At most one active route may say yes.</param>
    /// <param name="IsPublished">Whether submissions may pick it up at all.</param>
    /// <param name="IsActive">Whether it is in use.</param>
    /// <param name="EffectiveFromUtc">Not before this.</param>
    /// <param name="EffectiveToUtc">Not after this.</param>
    /// <param name="Stages">Its levels, in order.</param>
    public sealed record Row(
        int Id,
        string WorkflowName,
        int VersionNumber,
        string? Description,
        int? ServiceTemplateId,
        int? LocationId,
        string? Priority,
        bool IsDefault,
        bool IsPublished,
        bool IsActive,
        DateTime? EffectiveFromUtc,
        DateTime? EffectiveToUtc,
        IReadOnlyList<Stage> Stages);

    /// <summary>One level.</summary>
    /// <param name="Id">The stage.</param>
    /// <param name="StageNumber">Its place in the order.</param>
    /// <param name="StageName">What the screen calls it.</param>
    /// <param name="ApprovalMode">Any or All.</param>
    /// <param name="DueAfterMinutes">How long it has.</param>
    /// <param name="EscalateAfterMinutes">How long after that before it goes up.</param>
    /// <param name="AllowDelegation">Whether it may be handed on.</param>
    /// <param name="IsEnabled">Whether it takes part.</param>
    /// <param name="Rules">How its approvers are found.</param>
    public sealed record Stage(
        int Id,
        int StageNumber,
        string StageName,
        string ApprovalMode,
        int? DueAfterMinutes,
        int? EscalateAfterMinutes,
        bool AllowDelegation,
        bool IsEnabled,
        IReadOnlyList<Rule> Rules);

    /// <summary>One approver rule.</summary>
    /// <param name="Id">The rule.</param>
    /// <param name="ResolverType">How it finds people.</param>
    /// <param name="ResolverUserId">The named person, for User.</param>
    /// <param name="ResolverRoleId">The role, for Role.</param>
    /// <param name="ResolverCapabilityName">The capability, for Capability and LocationBranchAdmin.</param>
    /// <param name="ResolverEmail">The address, for CustomEmail.</param>
    /// <param name="DisplayName">What to call them.</param>
    /// <param name="IsRequired">Whether an All level must wait for them.</param>
    /// <param name="IsEnabled">Whether the rule takes part.</param>
    public sealed record Rule(
        int Id,
        string ResolverType,
        int? ResolverUserId,
        int? ResolverRoleId,
        string? ResolverCapabilityName,
        string? ResolverEmail,
        string? DisplayName,
        bool IsRequired,
        bool IsEnabled);
}
