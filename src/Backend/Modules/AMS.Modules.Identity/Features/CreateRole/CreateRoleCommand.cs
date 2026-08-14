using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.CreateRole;

/// <summary>
/// Add a role. Catalogue screen: Roles &amp; Capabilities.
/// </summary>
public sealed record CreateRoleCommand(
    string RoleName,
    string? Description) : ICommand<CreateRoleResponse>;
