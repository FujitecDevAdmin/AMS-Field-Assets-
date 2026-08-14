namespace AMS.Modules.Discovery.Features.ResolveDiscoveredDevice;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class ResolveDiscoveredDeviceMapper
{
    public static ResolveDiscoveredDeviceCommand ToCommand(ResolveDiscoveredDeviceRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ResolveDiscoveredDeviceCommand(
            id,
            request.Status.Trim(),
            request.LinkedAssetId);
    }
}
