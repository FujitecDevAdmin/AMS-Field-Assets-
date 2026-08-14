namespace AMS.Modules.Discovery.Features.ResolveDiscoveredDevice;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record ResolveDiscoveredDeviceRequest(
    string Status,
    int? LinkedAssetId);
