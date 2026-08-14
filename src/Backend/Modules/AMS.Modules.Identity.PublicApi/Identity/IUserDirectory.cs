namespace AMS.Modules.Identity.PublicApi.Identity;

/// <summary>Who somebody is, and who holds a role or a capability.</summary>
/// <remarks>
/// Read-only and by design. Another module may need to know who to write to;
/// none of them may create a user, grant a capability, or change what somebody
/// can do. Those are Identity's own slices, behind Identity's own
/// capabilities.
/// </remarks>
public interface IUserDirectory
{
    /// <summary>One user, or null if the id is unknown.</summary>
    Task<UserContact?> FindAsync(int userId, CancellationToken ct);

    /// <summary>The user account behind an employee, if one exists.</summary>
    Task<UserContact?> ForEmployeeAsync(int employeeId, CancellationToken ct);

    /// <summary>Everybody currently holding a role. Active accounts only.</summary>
    Task<IReadOnlyList<UserContact>> InRoleAsync(int roleId, CancellationToken ct);

    /// <summary>
    /// Everybody currently holding a capability, however they hold it.
    /// </summary>
    /// <param name="capabilityName">The capability, spelled as the seed spells it.</param>
    /// <param name="branchId">
    /// When given, only users who can act at that branch — those scoped to it,
    /// plus those with all-branch access. Null means do not narrow.
    /// </param>
    /// <param name="ct">Cancellation.</param>
    Task<IReadOnlyList<UserContact>> WithCapabilityAsync(
        string capabilityName,
        int? branchId,
        CancellationToken ct);
}
