namespace AMS.Modules.Discovery.Features.SearchDiscoveredDevices;

/// <summary>
/// One page of machines, most recently seen first.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">Devices matching the filter.</param>
/// <param name="UnresolvedCount">How many nobody has decided about. The queue length.</param>
public sealed record SearchDiscoveredDevicesResponse(
    IReadOnlyList<SearchDiscoveredDevicesResponse.Row> Rows,
    int TotalCount,
    int UnresolvedCount)
{
    /// <summary>One machine the agent found.</summary>
    /// <param name="Id">The device row.</param>
    /// <param name="Hostname">What it calls itself.</param>
    /// <param name="SerialNumber">What the manufacturer calls it.</param>
    /// <param name="Manufacturer">Who made it.</param>
    /// <param name="Model">What model.</param>
    /// <param name="OperatingSystem">What it runs.</param>
    /// <param name="MacAddress">Its network card.</param>
    /// <param name="Status">New, Linked, Registered or Ignored.</param>
    /// <param name="LinkedAssetId">The asset it belongs to, once somebody has said so.</param>
    /// <param name="FirstSeenOnUtc">The first time it reported.</param>
    /// <param name="LastSeenOnUtc">The last time. A machine that stopped reporting is worth a look.</param>
    public sealed record Row(
        int Id,
        string Hostname,
        string? SerialNumber,
        string? Manufacturer,
        string? Model,
        string? OperatingSystem,
        string? MacAddress,
        string Status,
        int? LinkedAssetId,
        DateTime FirstSeenOnUtc,
        DateTime LastSeenOnUtc);
}
