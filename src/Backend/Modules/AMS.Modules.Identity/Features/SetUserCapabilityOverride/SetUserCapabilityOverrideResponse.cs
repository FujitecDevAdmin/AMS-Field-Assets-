namespace AMS.Modules.Identity.Features.SetUserCapabilityOverride;

/// <summary>
/// The override now in force.
/// </summary>
/// <param name="UserId">The user.</param>
/// <param name="CapabilityName">The capability.</param>
/// <param name="IsGranted">False is a DENY, and a deny beats every role grant. That is the point: one permission can be withdrawn without unpicking roles.</param>
public sealed record SetUserCapabilityOverrideResponse(
    int UserId,
    string CapabilityName,
    bool IsGranted);
