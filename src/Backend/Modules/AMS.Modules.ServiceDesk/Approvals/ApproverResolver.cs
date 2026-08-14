using AMS.Modules.Identity.PublicApi.Identity;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.Modules.ServiceDesk.Domain;

namespace AMS.Modules.ServiceDesk.Approvals;

/// <summary>One person a rule resolved to, as they were at that moment.</summary>
/// <param name="UserId">Their account, if they have one.</param>
/// <param name="EmployeeId">Their employee record, if known.</param>
/// <param name="Name">What to call them.</param>
/// <param name="Email">Where to write. Empty when nothing could be found.</param>
public sealed record ResolvedApprover(int? UserId, int? EmployeeId, string Name, string Email);

/// <summary>
/// Turns the approver RULES on a stage into the actual people who must decide.
/// </summary>
/// <remarks>
/// <para>
/// This runs exactly once per run, at submission, and what it produces is
/// snapshotted into <c>RequestApprovalParticipant</c> — name and address
/// included. A manager who changes job, a role that is re-scoped, an account
/// that is deleted: none of them may rewrite who was asked to approve
/// something last month. The rule is the question; the participant row is the
/// answer, and only the answer is evidence.
/// </para>
/// <para>
/// Everything it needs from other modules comes through their PublicApi
/// contracts (rule 3). ServiceDesk cannot see Identity's tables and does not
/// want to: what it needs is a name and an address, not a user record.
/// </para>
/// </remarks>
public sealed class ApproverResolver(IUserDirectory users, IEmployeeDirectory employees)
{
    /// <summary>
    /// Resolves every enabled rule on a stage, in rule order, dropping
    /// duplicates.
    /// </summary>
    /// <param name="rules">The stage's rules.</param>
    /// <param name="context">Who the request is for and where.</param>
    /// <param name="ct">Cancellation.</param>
    /// <remarks>
    /// The same person reached by two rules is one approver, not two: asking
    /// somebody twice and then waiting for both answers is a level that can
    /// never complete. The FIRST rule that finds them wins, because
    /// UX_RequestApprovalParticipant_Resolved is on
    /// (step, rule, email) and the participant has to point at one rule.
    /// </remarks>
    public async Task<IReadOnlyList<(ApprovalStageApproverRule Rule, ResolvedApprover Approver)>>
        ResolveAsync(
            IEnumerable<ApprovalStageApproverRule> rules,
            ApprovalContext context,
            CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(context);

        var resolved = new List<(ApprovalStageApproverRule, ResolvedApprover)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in rules.Where(r => r.IsEnabled))
        {
            foreach (var approver in await ResolveOneAsync(rule, context, ct))
            {
                // Keyed on the address because that is what the unique index
                // and the notification both use, and an external approver has
                // nothing else.
                if (seen.Add(approver.Email))
                {
                    resolved.Add((rule, approver));
                }
            }
        }

        return resolved;
    }

    private async Task<IReadOnlyList<ResolvedApprover>> ResolveOneAsync(
        ApprovalStageApproverRule rule,
        ApprovalContext context,
        CancellationToken ct) => rule.ResolverType switch
    {
        ResolverType.User => From(
            rule.ResolverUserId is { } id ? await users.FindAsync(id, ct) : null, rule),

        ResolverType.Role => Many(
            rule.ResolverRoleId is { } id ? await users.InRoleAsync(id, ct) : []),

        ResolverType.Capability => Many(
            rule.ResolverCapabilityName is { } name
                ? await users.WithCapabilityAsync(name, null, ct)
                : []),

        // Narrowed to the branch the request came from. A branch admin in
        // Chennai approving a Coimbatore joiner is exactly what the narrowing
        // exists to prevent.
        ResolverType.LocationBranchAdmin => Many(
            rule.ResolverCapabilityName is { } name
                ? await users.WithCapabilityAsync(name, context.LocationId, ct)
                : []),

        ResolverType.EmployeeManager => From(
            await ManagerOfAsync(context.OnBehalfOfEmployeeId ?? context.RequestedByEmployeeId, ct),
            rule),

        ResolverType.RequesterManager => From(
            await ManagerOfAsync(context.RequestedByEmployeeId, ct), rule),

        // Somebody with no account at all: a landlord, an auditor, a vendor.
        // The address IS the identity, which is why
        // CK_RequestApprovalParticipant_Identity accepts a row with neither a
        // user nor an employee as long as the address is not empty.
        ResolverType.CustomEmail => rule.ResolverEmail is { } email
            ? [new ResolvedApprover(null, null, rule.DisplayName ?? email, email)]
            : [],

        _ => [],
    };

    private async Task<UserContact?> ManagerOfAsync(int? employeeId, CancellationToken ct)
    {
        if (employeeId is not { } employee)
        {
            return null;
        }

        var managerId = await employees.ManagerOfAsync(employee, ct);

        return managerId is { } manager ? await users.ForEmployeeAsync(manager, ct) : null;
    }

    private static IReadOnlyList<ResolvedApprover> From(
        UserContact? contact,
        ApprovalStageApproverRule rule) =>
        contact is null || string.IsNullOrWhiteSpace(contact.Email)
            // Silently dropping somebody with no address would produce a level
            // waiting on nobody. Returning nothing lets the caller say so.
            ? []
            : [new ResolvedApprover(
                contact.UserId, contact.EmployeeId,
                rule.DisplayName ?? contact.DisplayName, contact.Email)];

    private static IReadOnlyList<ResolvedApprover> Many(IReadOnlyList<UserContact> contacts) =>
        [.. contacts
            .Where(c => !string.IsNullOrWhiteSpace(c.Email))
            .Select(c => new ResolvedApprover(c.UserId, c.EmployeeId, c.DisplayName, c.Email!))];
}

/// <summary>What the resolvers need to know about the request being approved.</summary>
/// <param name="RequestedByEmployeeId">Who raised it.</param>
/// <param name="OnBehalfOfEmployeeId">Who it is for, when that is somebody else.</param>
/// <param name="LocationId">Where. Narrows LocationBranchAdmin.</param>
public sealed record ApprovalContext(
    int RequestedByEmployeeId,
    int? OnBehalfOfEmployeeId,
    int? LocationId);
