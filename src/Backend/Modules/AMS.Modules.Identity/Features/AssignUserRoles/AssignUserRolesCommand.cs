using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.AssignUserRoles;

/// <summary>
/// Replace the roles a user holds. Catalogue: Assign roles.
/// </summary>
public sealed record AssignUserRolesCommand(
    int UserId,
    IReadOnlyList<int> RoleIds) : ICommand<AssignUserRolesResponse>;
