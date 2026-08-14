using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Discovery.Features.SearchDiscoveredDevices;

/// <summary>
/// Machines the agent has found. Catalogue: Discovered Devices.
/// </summary>
public sealed record SearchDiscoveredDevicesQuery(
    string? Status,
    string? Search,
    bool UnresolvedOnly,
    int Skip,
    int Take) : IQuery<SearchDiscoveredDevicesResponse>;
