namespace AMS.Modules.ServiceLevel.Features.SetSlaEscalations;

/// <summary>
/// The ladder as it now stands.
/// </summary>
/// <param name="SlaPolicyId">The policy.</param>
/// <param name="ResponseLevelCount">How many levels chase a missed response.</param>
/// <param name="ResolutionLevelCount">How many chase a missed resolution.</param>
public sealed record SetSlaEscalationsResponse(
    int SlaPolicyId,
    int ResponseLevelCount,
    int ResolutionLevelCount);
