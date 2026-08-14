namespace AMS.Modules.Identity.Features.SetRoleCapabilities;

/// <summary>
/// What the role grants now.
/// </summary>
/// <param name="RoleId">The role changed.</param>
/// <param name="CapabilityNames">The complete set afterwards, not a delta.</param>
public sealed record SetRoleCapabilitiesResponse(
    int RoleId,
    IReadOnlyList<string> CapabilityNames);
