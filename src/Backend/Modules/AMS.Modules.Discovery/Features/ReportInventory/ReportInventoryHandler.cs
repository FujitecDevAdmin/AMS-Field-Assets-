using AMS.Modules.Discovery.Agents;
using AMS.Modules.Discovery.Domain;
using AMS.Modules.Discovery.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Discovery.Features.ReportInventory;

/// <summary>
/// What an agent found on one machine.
/// </summary>
/// <remarks>
/// <para>
/// Posted by software, not by a person, and posted often — every machine in the
/// company on a schedule. Three things follow from that.
/// </para>
/// <para>
/// <b>It authenticates with an API key, not a session.</b> An agent has no
/// user, no branches and nobody to grant a capability to. It presents a key,
/// the key is looked up by its prefix and compared by hash, and holding a live
/// one is the whole of the authorisation.
/// </para>
/// <para>
/// <b>It is an upsert, not an insert.</b> The same machine reports for years.
/// A device row is matched on hostname and serial, health is one row per asset
/// that is overwritten with a history entry kept, and software is reconciled
/// against what was there last time.
/// </para>
/// <para>
/// <b>It decides nothing.</b> A newly seen machine lands in the queue as
/// <c>New</c> and waits for a person: it may be a contractor's laptop, a test
/// rig, or something already on the register under another name. An agent that
/// created assets would fill the register with them.
/// </para>
/// </remarks>
public sealed class ReportInventoryHandler(
    DiscoveryDbContext db,
    IClock clock)
    : IRequestHandler<ReportInventoryCommand, ReportInventoryResponse>
{
    public async Task<Result<ReportInventoryResponse>> HandleAsync(
        ReportInventoryCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var key = await AuthenticateAsync(request.ApiKey, ct);
        if (key is null)
        {
            // One message for every failure: no key, an unknown key, a revoked
            // key. Telling an agent WHICH would tell anybody probing the
            // endpoint which of their guesses was closest.
            //
            // Forbidden rather than a 401, matching what SignIn does with a bad
            // password. The kernel has no Unauthorized kind, and inventing one
            // for this endpoint alone would leave the codebase answering the
            // same question two ways.
            return Error.Forbidden(
                "Agent.KeyRejected",
                "That agent key was not accepted.");
        }

        var now = clock.UtcNow;

        key.LastUsedOnUtc = now;

        var device = await FindDeviceAsync(request, ct);
        var isNew = device is null;

        if (device is null)
        {
            device = new DiscoveredDevice
            {
                Hostname = request.Hostname,
                SerialNumber = request.SerialNumber,
                // Nobody has looked at it yet. It is not an asset until
                // somebody says so.
                Status = DiscoveredDeviceStatus.New,
                FirstSeenOnUtc = now,
                LastSeenOnUtc = now,
                CreatedOnUtc = now,
                CreatedBy = "agent",
            };

            db.DiscoveredDevices.Add(device);
        }
        else
        {
            device.ModifiedOnUtc = now;
            device.ModifiedBy = "agent";
        }

        // Hardware facts are refreshed every time: a machine that had its
        // memory replaced or its OS upgraded should read as it is now.
        device.Manufacturer = request.Manufacturer ?? device.Manufacturer;
        device.Model = request.Model ?? device.Model;
        device.OperatingSystem = request.OperatingSystem ?? device.OperatingSystem;
        device.MacAddress = request.MacAddress ?? device.MacAddress;
        device.SerialNumber ??= request.SerialNumber;
        device.RawPayloadJson = request.RawPayloadJson ?? device.RawPayloadJson;
        device.LastSeenOnUtc = now;

        await db.SaveChangesAsync(ct);

        // Health and software hang off the ASSET, not the device, because they
        // are facts about a thing on the register. An unlinked machine reports
        // them and they are kept against the device only - there is nowhere
        // else to put them until somebody says what it is.
        var assetId = device.LinkedAssetId ?? request.AssetId;

        var recorded = 0;
        var removed = 0;

        if (assetId is { } asset)
        {
            await RecordHealthAsync(asset, device.Hostname, request.Health, now, ct);
            (recorded, removed) = await ReconcileSoftwareAsync(asset, request.Software, now, ct);
        }

        await db.SaveChangesAsync(ct);

        return new ReportInventoryResponse(
            device.Id, device.Status, isNew, device.LinkedAssetId, recorded, removed);
    }

    /// <summary>
    /// The live key behind a presented secret, or null.
    /// </summary>
    /// <remarks>
    /// Found by prefix, confirmed by hash. The prefix is what makes this one
    /// indexed read rather than a hash of every key in the table on every post.
    /// </remarks>
    private async Task<AgentApiKey?> AuthenticateAsync(string? presented, CancellationToken ct)
    {
        if (AgentKeys.PrefixOf(presented) is not { } prefix)
        {
            return null;
        }

        var candidates = await db.AgentApiKeys
            .Where(k => k.KeyPrefix == prefix && k.IsActive)
            .ToListAsync(ct);

        return candidates.Find(k => AgentKeys.Matches(presented!, k.KeyHash));
    }

    /// <summary>
    /// The device this report belongs to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Serial first, because a machine keeps its serial when somebody renames
    /// it and hostnames are reused — the laptop handed to the next joiner is
    /// often called the same thing.
    /// </para>
    /// <para>
    /// When the report carries a serial nobody has seen, the fallback is a
    /// device with the same hostname AND no serial on file: that is a machine
    /// we knew before the agent learned to read its serial, and the field is
    /// filled in rather than a second row created.
    /// </para>
    /// <para>
    /// When the report carries NO serial, the fallback is the hostname alone.
    /// An agent that read the serial last week and cannot read it today — a
    /// driver update, a permissions change, a virtual machine — is still the
    /// same machine, and insisting on the serial would quietly split its
    /// history in two.
    /// </para>
    /// </remarks>
    private async Task<DiscoveredDevice?> FindDeviceAsync(
        ReportInventoryCommand request,
        CancellationToken ct)
    {
        if (request.SerialNumber is { Length: > 0 } serial)
        {
            var bySerial = await db.DiscoveredDevices
                .FirstOrDefaultAsync(d => d.SerialNumber == serial, ct);

            return bySerial ?? await db.DiscoveredDevices
                .FirstOrDefaultAsync(
                    d => d.Hostname == request.Hostname && d.SerialNumber == null, ct);
        }

        // Most recently seen, because a reused hostname leaves more than one
        // and the live machine is the one that reported last.
        return await db.DiscoveredDevices
            .Where(d => d.Hostname == request.Hostname)
            .OrderByDescending(d => d.LastSeenOnUtc)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// One current row per asset, and a history entry beside it.
    /// </summary>
    /// <remarks>
    /// <c>AssetHealth</c> is keyed on the asset and overwritten, because a
    /// screen asking "how is this machine" wants one answer.
    /// <c>AssetHealthHistory</c> keeps the readings, because a drive that has
    /// been filling for a month is a different problem from one that filled
    /// yesterday, and only the trend tells them apart.
    /// </remarks>
    private async Task RecordHealthAsync(
        int assetId,
        string hostname,
        ReportInventoryCommand.HealthReading? reading,
        DateTime now,
        CancellationToken ct)
    {
        if (reading is null)
        {
            return;
        }

        var health = await db.AssetHealths.SingleOrDefaultAsync(h => h.AssetId == assetId, ct);

        if (health is null)
        {
            health = new AssetHealth { AssetId = assetId, Hostname = hostname };
            db.AssetHealths.Add(health);
        }

        health.Hostname = hostname;
        health.CpuPercent = reading.CpuPercent;
        health.MemoryPercent = reading.MemoryPercent;
        health.SystemDrivePercent = reading.SystemDrivePercent;
        health.BatteryHealthPercent = reading.BatteryHealthPercent;
        health.UptimeHours = reading.UptimeHours;
        health.LoggedInUser = reading.LoggedInUser;
        health.LastSeenOnUtc = now;

        db.AssetHealthHistories.Add(new AssetHealthHistory
        {
            AssetId = assetId,
            CpuPercent = reading.CpuPercent,
            MemoryPercent = reading.MemoryPercent,
            SystemDrivePercent = reading.SystemDrivePercent,
            CapturedOnUtc = now,
        });
    }

    /// <summary>
    /// What is installed now against what was installed last time.
    /// </summary>
    /// <remarks>
    /// Uninstalled software is marked <c>IsRemoved</c> rather than deleted. A
    /// licence audit asks what was installed during a period, not what is
    /// installed today, and a row that vanished when somebody uninstalled it
    /// cannot answer.
    ///
    /// A title that comes back is un-removed rather than duplicated —
    /// <c>UX_AssetInstalledSoftware_Install</c> is on (asset, name, version)
    /// and would refuse the second row anyway.
    /// </remarks>
    private async Task<(int Recorded, int Removed)> ReconcileSoftwareAsync(
        int assetId,
        IReadOnlyList<ReportInventoryCommand.SoftwareEntry> reported,
        DateTime now,
        CancellationToken ct)
    {
        if (reported.Count == 0)
        {
            // An agent that could not enumerate software sends nothing, and
            // nothing must not be read as "everything was uninstalled".
            return (0, 0);
        }

        var existing = await db.AssetInstalledSoftwares
            .Where(s => s.AssetId == assetId)
            .ToListAsync(ct);

        var seen = new HashSet<(string Name, string? Version)>();

        foreach (var entry in reported)
        {
            seen.Add((entry.SoftwareName, entry.Version));

            var row = existing.Find(s =>
                s.SoftwareName == entry.SoftwareName && s.Version == entry.Version);

            if (row is null)
            {
                db.AssetInstalledSoftwares.Add(new AssetInstalledSoftware
                {
                    AssetId = assetId,
                    SoftwareName = entry.SoftwareName,
                    Version = entry.Version,
                    Publisher = entry.Publisher,
                    FirstSeenOnUtc = now,
                    LastSeenOnUtc = now,
                    IsRemoved = false,
                });

                continue;
            }

            row.Publisher = entry.Publisher ?? row.Publisher;
            row.LastSeenOnUtc = now;
            row.IsRemoved = false;
        }

        var removed = 0;

        foreach (var row in existing.Where(s =>
                     !s.IsRemoved && !seen.Contains((s.SoftwareName, s.Version))))
        {
            row.IsRemoved = true;
            removed++;
        }

        return (reported.Count, removed);
    }
}
