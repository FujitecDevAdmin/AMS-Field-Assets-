namespace AMS.Modules.Discovery.Features.SearchDiscoveredDevices;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchDiscoveredDevicesRequest(
    string? Status,
    string? Search,
    bool? UnresolvedOnly,
    int? Skip,
    int? Take);
