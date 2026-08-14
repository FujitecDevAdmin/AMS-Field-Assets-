namespace AMS.Modules.Discovery.Features.ReportInventory;

/// <summary>
/// What the report did.
/// </summary>
/// <param name="DiscoveredDeviceId">The device row, new or updated.</param>
/// <param name="Status">New, Linked, Registered or Ignored.</param>
/// <param name="IsNewDevice">True the first time a machine reports.</param>
/// <param name="LinkedAssetId">The asset it belongs to, once somebody has said so.</param>
/// <param name="SoftwareRecorded">How many installations were seen this time.</param>
/// <param name="SoftwareRemoved">How many previously seen installations have gone.</param>
public sealed record ReportInventoryResponse(
    int DiscoveredDeviceId,
    string Status,
    bool IsNewDevice,
    int? LinkedAssetId,
    int SoftwareRecorded,
    int SoftwareRemoved);
