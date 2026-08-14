using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.PublicApi;

namespace AMS.Modules.Assets.Persistence;

/// <summary>
/// Writes <c>Assets.AssetEvent</c> on behalf of whichever module is making the
/// change. See <see cref="IAssetTimeline"/> for why this exists.
/// </summary>
public sealed class AssetTimeline(AssetsDbContext db) : IAssetTimeline
{
    public async Task AppendAsync(AssetTimelineEntry entry, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entry);

        db.AssetEvents.Add(new AssetEvent
        {
            AssetId = entry.AssetId,
            EventType = entry.EventType,
            Description = entry.Description,
            EventOnUtc = entry.EventOnUtc,
            PerformedBy = entry.PerformedBy,
            EmployeeId = entry.EmployeeId,
            EmployeeNameSnapshot = entry.EmployeeNameSnapshot,
            LocationId = entry.LocationId,
            LocationNameSnapshot = entry.LocationNameSnapshot,
            AllocationId = entry.AllocationId,
            MovementId = entry.MovementId,
            ServiceRequestId = entry.ServiceRequestId,
            ContractId = entry.ContractId,
            HandoverId = entry.HandoverId,
            VerificationId = entry.VerificationId,
        });

        // Saves its OWN context, and must.
        //
        // It used to stage the row and leave saving to "the calling handler's
        // transaction". That works only while the caller is inside this module
        // and holds the same AssetsDbContext. From any other module - which is
        // the entire point of this contract - the caller saves a DIFFERENT
        // context and the row is silently dropped: the allocation succeeds and
        // its history never existed.
        //
        // Saving is not committing. Rule 4a puts every module context in a
        // request on one transaction owned by the dispatcher, so a command that
        // fails still takes this row with it. That is what
        // A_failed_change_takes_its_timeline_row_with_it proves.
        await db.SaveChangesAsync(ct);
    }
}
