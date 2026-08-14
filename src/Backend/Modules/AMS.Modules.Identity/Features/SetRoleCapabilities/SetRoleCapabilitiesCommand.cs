using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.SetRoleCapabilities;

/// <summary>
/// Replace the capabilities a role grants. Catalogue: the capability matrix.
/// </summary>
public sealed record SetRoleCapabilitiesCommand(
    int RoleId,
    IReadOnlyList<string> CapabilityNames) : ICommand<SetRoleCapabilitiesResponse>;
