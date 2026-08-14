namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>How a level decides. Spelled as CK_ApprovalWorkflowStage_Mode allows.</summary>
public static class ApprovalMode
{
    /// <summary>One approval finishes the level; one rejection rejects it.</summary>
    public const string Any = "Any";

    /// <summary>Every required approver must approve. One rejection still rejects it.</summary>
    public const string All = "All";

    public static readonly string[] Allowed = [Any, All];
}

/// <summary>
/// How a stage's approvers are found. Spelled as
/// CK_ApprovalStageApproverRule_ResolverType allows.
/// </summary>
/// <remarks>
/// Resolution happens once, at submission. What is stored on the rule is the
/// QUESTION; the answer is snapshotted into RequestApprovalParticipant, so a
/// promotion or a leaver cannot rewrite who was asked.
/// </remarks>
public static class ResolverType
{
    /// <summary>A named person.</summary>
    public const string User = "User";

    /// <summary>Everybody holding a role.</summary>
    public const string Role = "Role";

    /// <summary>Everybody holding a capability, wherever they are.</summary>
    public const string Capability = "Capability";

    /// <summary>The manager of the person the request is FOR.</summary>
    public const string EmployeeManager = "EmployeeManager";

    /// <summary>The manager of the person who RAISED it.</summary>
    public const string RequesterManager = "RequesterManager";

    /// <summary>Holders of a capability, narrowed to the request's branch.</summary>
    public const string LocationBranchAdmin = "LocationBranchAdmin";

    /// <summary>Somebody with no login at all — an external approver, by e-mail.</summary>
    public const string CustomEmail = "CustomEmail";

    public static readonly string[] Allowed =
        [User, Role, Capability, EmployeeManager, RequesterManager, LocationBranchAdmin, CustomEmail];
}

/// <summary>Where a run has got to. CK_RequestApprovalInstance_Status.</summary>
public static class ApprovalInstanceStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
}

/// <summary>Where one level has got to. CK_RequestApprovalStep_Status.</summary>
public static class ApprovalStepStatus
{
    /// <summary>Its turn has not come. CK_RequestApprovalStep_Activation lets this one have no activation time.</summary>
    public const string Waiting = "Waiting";

    /// <summary>Its turn. Exactly one step per run may be here — UX_RequestApprovalStep_OnePending.</summary>
    public const string Pending = "Pending";

    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    /// <summary>Passed over deliberately, and always recorded in history.</summary>
    public const string Skipped = "Skipped";

    /// <summary>The run was called off before this level's turn came.</summary>
    public const string Cancelled = "Cancelled";
}

/// <summary>Where one approver stands. CK_RequestApprovalParticipant_Status.</summary>
public static class ParticipantStatus
{
    public const string Waiting = "Waiting";
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Delegated = "Delegated";
    public const string Cancelled = "Cancelled";
}

/// <summary>How a decision reached us. CK_RequestApprovalDecision_Source.</summary>
/// <remarks>
/// Recorded because an approval clicked in a mail client and one made in the
/// application are not equally strong evidence, and an audit that cannot tell
/// them apart cannot say so.
/// </remarks>
public static class DecisionSource
{
    public const string Application = "Application";
    public const string EmailLink = "EmailLink";
    public const string Api = "Api";

    public static readonly string[] Allowed = [Application, EmailLink, Api];
}

/// <summary>What a decision was. CK_RequestApprovalDecision_Decision.</summary>
public static class ApprovalDecision
{
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}
