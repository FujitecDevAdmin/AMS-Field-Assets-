using AMS.Modules.ServiceLevel.PublicApi.ServiceLevel;

namespace AMS.Modules.ServiceDesk.Tests;

/// <summary>
/// ServiceLevel's answers, as far as ServiceDesk is concerned.
/// </summary>
/// <remarks>
/// <para>
/// A stub, because what a working week is and how a policy treats it are
/// ServiceLevel's questions — tested there, against a real calendar, with no
/// database at all. What ServiceDesk has to get right is what it does with the
/// answers, and that is testable by stating them.
/// </para>
/// <para>
/// The default is "no policy configured": null targets, wall-clock minutes.
/// That is a real site's starting state, and it is what every ticket test that
/// does not care about SLA should see.
/// </para>
/// </remarks>
public sealed class FakeSlaCalculator : ISlaCalculator
{
    private SlaTargets? _targets;
    private int? _fixedMinutes;

    /// <summary>Makes the next ticket come back with these targets.</summary>
    public FakeSlaCalculator Returns(SlaTargets targets)
    {
        _targets = targets;

        return this;
    }

    /// <summary>
    /// Makes every span measure the same, whatever its wall-clock length.
    /// </summary>
    /// <remarks>
    /// Stands in for a calendar: "those two days were a weekend" becomes
    /// <c>Measures(0)</c>, without this project needing to know what a weekend
    /// is.
    /// </remarks>
    public FakeSlaCalculator Measures(int minutes)
    {
        _fixedMinutes = minutes;

        return this;
    }

    /// <summary>Back to no policy and wall-clock minutes.</summary>
    public void Reset()
    {
        _targets = null;
        _fixedMinutes = null;
    }

    public Task<SlaTargets?> ComputeTargetsAsync(SlaTargetRequest request, CancellationToken ct) =>
        Task.FromResult(_targets);

    public Task<int> OperationalMinutesAsync(
        int? locationId,
        DateTime fromUtc,
        DateTime toUtc,
        int? slaPolicyId,
        CancellationToken ct) =>
        Task.FromResult(_fixedMinutes
            ?? (toUtc <= fromUtc ? 0 : (int)(toUtc - fromUtc).TotalMinutes));
}
