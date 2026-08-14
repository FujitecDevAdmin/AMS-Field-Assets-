using AMS.SharedKernel.Abstractions;

namespace AMS.Modules.Identity.Domain;

/// <summary>
/// A role grants a capability. Deleting the capability deletes the grant —
/// retiring a capability is a code-level decision and its grants mean nothing
/// without it (R2-6).
/// </summary>
public sealed class RoleCapability : IGrantable
{
    public int RoleId { get; set; }

    public required string CapabilityName { get; set; }

    public DateTime GrantedOnUtc { get; set; }

    public string? GrantedBy { get; set; }
}
