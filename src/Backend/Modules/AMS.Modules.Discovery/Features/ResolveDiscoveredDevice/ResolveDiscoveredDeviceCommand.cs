using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Discovery.Features.ResolveDiscoveredDevice;

/// <summary>
/// Say what a discovered machine is. Catalogue: Discovered Devices.
/// </summary>
public sealed record ResolveDiscoveredDeviceCommand(
    int Id,
    string Status,
    int? LinkedAssetId) : ICommand<ResolveDiscoveredDeviceResponse>;
