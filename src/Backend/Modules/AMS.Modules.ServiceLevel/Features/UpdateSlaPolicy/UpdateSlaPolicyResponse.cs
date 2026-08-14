namespace AMS.Modules.ServiceLevel.Features.UpdateSlaPolicy;

/// <summary>
/// The policy as it now stands.
/// </summary>
/// <param name="Id">The policy.</param>
/// <param name="PolicyName">What it is called.</param>
/// <param name="IsActive">Whether tickets of that priority are judged by it.</param>
public sealed record UpdateSlaPolicyResponse(
    int Id,
    string PolicyName,
    bool IsActive);
