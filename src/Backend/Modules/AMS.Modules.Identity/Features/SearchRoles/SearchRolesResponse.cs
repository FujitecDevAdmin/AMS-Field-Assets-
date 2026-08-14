namespace AMS.Modules.Identity.Features.SearchRoles;

/// <summary>
/// Every role matching the filter. Roles are few; this list is not paged.
/// </summary>
/// <param name="Rows">The roles.</param>
public sealed record SearchRolesResponse(IReadOnlyList<SearchRolesResponse.Row> Rows)
{
    /// <summary>One line of the role list.</summary>
    /// <param name="Id">The role.</param>
    /// <param name="RoleName">Unique, enforced by UX_Role_Name.</param>
    /// <param name="Description">May be null.</param>
    /// <param name="IsSystemRole">A role the application depends on; not deletable from the UI.</param>
    /// <param name="IsActive">An inactive role grants nothing.</param>
    /// <param name="CapabilityCount">How many capabilities it grants.</param>
    /// <param name="UserCount">How many people hold it. Retiring a role people hold is worth a warning.</param>
    public sealed record Row(
        int Id,
        string RoleName,
        string? Description,
        bool IsSystemRole,
        bool IsActive,
        int CapabilityCount,
        int UserCount);
}
