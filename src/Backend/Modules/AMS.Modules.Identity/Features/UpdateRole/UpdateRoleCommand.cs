using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.UpdateRole;

/// <summary>
/// Rename a role or deactivate it.
/// </summary>
public sealed record UpdateRoleCommand(
    int RoleId,
    string RoleName,
    string? Description,
    bool IsActive) : ICommand<UpdateRoleResponse>;
