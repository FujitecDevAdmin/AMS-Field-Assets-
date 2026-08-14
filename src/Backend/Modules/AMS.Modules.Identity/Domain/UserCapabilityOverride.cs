using AMS.SharedKernel.Abstractions;

namespace AMS.Modules.Identity.Domain;

/// <summary>
/// A per-user grant or deny that beats the role union in both directions.
/// </summary>
/// <remarks>
/// <see cref="IsGranted"/> = false is a DENY and must win, so one permission
/// can be taken away from one person without unpicking their roles.
/// </remarks>
public sealed class UserCapabilityOverride : IGrantable
{
    public int UserId { get; set; }

    public required string CapabilityName { get; set; }

    /// <summary>True grants, false denies. A deny beats every role grant.</summary>
    public bool IsGranted { get; set; }

    public string? Reason { get; set; }

    public DateTime GrantedOnUtc { get; set; }

    public string? GrantedBy { get; set; }
}
