namespace AMS.Modules.Discovery.Features.ResolveDiscoveredDevice;

/// <summary>
/// What was decided.
/// </summary>
/// <param name="Id">The device.</param>
/// <param name="Status">Linked, Registered or Ignored.</param>
/// <param name="LinkedAssetId">The asset it belongs to, when it belongs to one.</param>
public sealed record ResolveDiscoveredDeviceResponse(
    int Id,
    string Status,
    int? LinkedAssetId);
