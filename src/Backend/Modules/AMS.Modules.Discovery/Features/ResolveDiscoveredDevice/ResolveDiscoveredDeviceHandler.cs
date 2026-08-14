using AMS.Modules.Discovery.Domain;
using AMS.Modules.Discovery.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Discovery.Features.ResolveDiscoveredDevice;

/// <summary>Say what a discovered machine is. Catalogue: Discovered Devices.</summary>
/// <remarks>
/// An agent reporting a machine is not the same as somebody deciding it is an
/// asset: it may be a contractor's laptop, a test rig, or something already on
/// the register under a different name. This is where a person decides, and the
/// queue exists so nobody has to decide silently.
/// </remarks>
public sealed class ResolveDiscoveredDeviceHandler(
    DiscoveryDbContext db,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<ResolveDiscoveredDeviceCommand, ResolveDiscoveredDeviceResponse>
{
    public async Task<Result<ResolveDiscoveredDeviceResponse>> HandleAsync(
        ResolveDiscoveredDeviceCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!DiscoveredDeviceStatus.Resolved.Contains(request.Status, StringComparer.Ordinal))
        {
            return Error.Validation(
                "DiscoveredDevice.UnknownStatus",
                $"A decision is one of {string.Join(", ", DiscoveredDeviceStatus.Resolved)}.");
        }

        var device = await db.DiscoveredDevices.SingleOrDefaultAsync(d => d.Id == request.Id, ct);
        if (device is null)
        {
            return Error.NotFound("DiscoveredDevice", request.Id);
        }

        // Linked and Registered both mean "this machine IS that asset", so both
        // need to say which. Ignored means the opposite and must not carry one.
        var wantsAsset = request.Status is DiscoveredDeviceStatus.Linked
            or DiscoveredDeviceStatus.Registered;

        if (wantsAsset && request.LinkedAssetId is null)
        {
            return Error.Validation(
                "DiscoveredDevice.AssetRequired",
                $"Say which asset this machine is to mark it {request.Status}.");
        }

        if (!wantsAsset && request.LinkedAssetId is not null)
        {
            return Error.Validation(
                "DiscoveredDevice.AssetNotAllowed",
                "An ignored machine does not belong to an asset.");
        }

        var now = clock.UtcNow;

        device.Status = request.Status;
        device.LinkedAssetId = request.LinkedAssetId;
        device.ModifiedOnUtc = now;
        device.ModifiedBy = currentUser.Username;

        await db.SaveChangesAsync(ct);

        return new ResolveDiscoveredDeviceResponse(
            device.Id, device.Status, device.LinkedAssetId);
    }
}
