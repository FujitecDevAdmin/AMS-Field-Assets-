namespace AMS.Modules.Assets.PublicApi;

/// <summary>
/// Moves an asset between branches, on behalf of the module that caused it.
/// </summary>
/// <remarks>
/// <para>
/// <c>Asset.CurrentLocationId</c> lives in <c>[Assets]</c>, and Movements
/// cannot write it: one DbContext maps one schema (01 §2 rule 1), and one
/// module never references another (rule 2). So Assets publishes this, and the
/// implementation uses the Assets context — the same arrangement as
/// <see cref="IAssetTimeline"/>, and safe for the same reason: rule 4a puts
/// both contexts on one transaction, so the move commits with the receipt or
/// not at all.
/// </para>
/// <para>
/// <b>There is deliberately no "despatch" call.</b> An asset in transit belongs
/// to neither branch, and the design says why: marking it as arrived on
/// despatch makes it findable somewhere it is not. The branch changes on
/// RECEIPT, once, which is the only moment anybody can say where the thing
/// actually is.
/// </para>
/// </remarks>
public interface IAssetCustody
{
    /// <summary>
    /// Records that an asset has arrived at a branch.
    /// </summary>
    /// <param name="assetId">The asset that arrived.</param>
    /// <param name="locationId">Where it now is.</param>
    /// <param name="ct">Cancellation.</param>
    /// <returns>
    /// False when no such asset exists, or it is deleted. The caller decides
    /// what that means — a receipt against an unknown asset is a 404, not an
    /// exception, because it is a thing a user can cause by typing.
    /// </returns>
    Task<bool> ReceiveAtLocationAsync(int assetId, int locationId, CancellationToken ct);

    /// <summary>
    /// Applies a completed transfer to the asset.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every argument is optional and only the ones supplied are changed, so a
    /// cost-centre transfer does not have to restate who holds the asset. A
    /// null means "leave it alone", never "clear it" — clearing custody is
    /// Allocations' business and has its own screen.
    /// </para>
    /// <para>
    /// <b>Branch is here and despatch is not</b>, and the difference is real: a
    /// transfer's branch change is an accounting fact recorded when somebody
    /// completes the paperwork, and any physical shipment it causes is a
    /// separate movement that moves the asset again on arrival.
    /// </para>
    /// </remarks>
    /// <returns>False when no such asset exists, or it is deleted.</returns>
    Task<bool> ApplyTransferAsync(
        int assetId,
        int? employeeId,
        int? departmentId,
        int? locationId,
        string? costCenter,
        CancellationToken ct);
}
