namespace AMS.Modules.ServiceLevel.Features.CreateSlaPolicy;

/// <summary>
/// The policy, live for its priority.
/// </summary>
/// <param name="Id">The policy.</param>
/// <param name="PolicyName">What it is called.</param>
/// <param name="Priority">The priority it covers. Only one active policy may.</param>
public sealed record CreateSlaPolicyResponse(
    int Id,
    string PolicyName,
    string Priority);
