namespace AMS.Modules.Assets.PublicApi;

/// <summary>
/// Reads the few asset facts other modules legitimately need.
/// </summary>
/// <remarks>
/// <para>
/// The read counterpart to <see cref="IAssetCustody"/>, and the contract doc 01
/// §2 rule 3 names as its own example. A module that needs to know where an
/// asset is cannot query <c>[Assets]</c>, so it asks.
/// </para>
/// <para>
/// Deliberately narrow. It returns the custody columns and nothing else — no
/// finance, no custom fields, no timeline. A snapshot that returned everything
/// would become the way other modules read the register, and the boundary
/// would exist only on paper.
/// </para>
/// </remarks>
public interface IAssetSnapshot
{
    /// <summary>
    /// One asset's custody facts, or null when it does not exist or is deleted.
    /// </summary>
    Task<AssetSnapshot?> GetAsync(int assetId, CancellationToken ct);
}

/// <summary>Where an asset is and whose it is, right now.</summary>
/// <param name="AssetId">The asset.</param>
/// <param name="AssetNumber">For a message a person has to read.</param>
/// <param name="CurrentEmployeeId">Who holds it, or null.</param>
/// <param name="CurrentLocationId">
/// Which branch, or null. Null is normal and means something: a bulk line has
/// no single location, and an asset in transit belongs to neither end.
/// </param>
/// <param name="DepartmentId">Which department, or null.</param>
/// <param name="CostCenter">Which cost centre carries it, or null.</param>
/// <param name="IsBulk">Whether it is a counted line rather than one thing.</param>
public sealed record AssetSnapshot(
    int AssetId,
    string AssetNumber,
    int? CurrentEmployeeId,
    int? CurrentLocationId,
    int? DepartmentId,
    string? CostCenter,
    bool IsBulk);
