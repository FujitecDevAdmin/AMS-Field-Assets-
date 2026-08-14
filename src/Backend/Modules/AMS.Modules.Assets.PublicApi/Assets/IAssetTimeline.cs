namespace AMS.Modules.Assets.PublicApi;

/// <summary>
/// Appends to an asset's business timeline.
/// </summary>
/// <remarks>
/// <para>
/// <c>Assets.AssetEvent</c> is written by nearly every module — Allocations on
/// allocation and handover, Movements on despatch and receipt, ServiceDesk when
/// a fault is raised, Verification when somebody stands in front of the asset.
/// None of them may touch this table: one DbContext maps one schema
/// (docs/01 §2 rule 1).
/// </para>
/// <para>
/// So this contract exists, and rule 4a is what makes it safe. The
/// implementation uses the Assets context, and <c>UnitOfWorkBehavior</c> has
/// already enlisted both contexts in one transaction on one connection. The
/// timeline row and the change it describes commit together or not at all.
/// </para>
/// <para>
/// It does not save. The calling handler's transaction owns that, which is the
/// entire point — an <c>Append</c> that committed on its own would produce
/// exactly the timeline-disagrees-with-the-record problem the design calls
/// worse than no timeline.
/// </para>
/// </remarks>
public interface IAssetTimeline
{
    /// <summary>
    /// Writes one timeline entry. It becomes permanent when the calling
    /// command's transaction commits, and disappears with it if that command
    /// fails.
    /// </summary>
    Task AppendAsync(AssetTimelineEntry entry, CancellationToken ct);
}
