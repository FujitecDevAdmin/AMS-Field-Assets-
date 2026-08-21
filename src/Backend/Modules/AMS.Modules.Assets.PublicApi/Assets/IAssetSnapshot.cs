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

    /// <summary>Active asset snapshots for a bounded set of identifiers.</summary>
    Task<IReadOnlyList<AssetSnapshot>> GetManyAsync(
        IReadOnlyCollection<int> assetIds,
        CancellationToken ct);

    /// <summary>Records the most recent successful physical verification.</summary>
    Task RecordPhysicalCheckAsync(int assetId, DateTime verifiedOnUtc, CancellationToken ct);

    /// <summary>Finds an active asset by its asset number, QR value, or barcode value.</summary>
    Task<AssetSnapshot?> FindByScanCodeAsync(string scanCode, CancellationToken ct);

    /// <summary>
    /// Number of active assets whose imported/custom branch matches one of the
    /// supplied Branch Master code/name aliases.
    /// </summary>
    Task<int> CountByImportedBranchesAsync(
        IReadOnlyCollection<int> branchIds,
        IReadOnlyCollection<string> branchAliases,
        CancellationToken ct);

    /// <summary>Active assets within the supplied imported Branch values.</summary>
    Task<IReadOnlyList<AssetSnapshot>> ListByImportedBranchesAsync(
        IReadOnlyCollection<int> branchIds,
        IReadOnlyCollection<string> branchAliases,
        CancellationToken ct);
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
/// <param name="AssetName">Business display name.</param>
/// <param name="SerialNumber">Manufacturer serial number, when recorded.</param>
/// <param name="Quantity">Expected register quantity.</param>
/// <param name="ImportedBranch">Branch value retained from the imported FAR row.</param>
/// <param name="ImportedLocation">Location value retained from the imported FAR row.</param>
/// <param name="QrCodeValue">Printed QR identifier, when assigned.</param>
/// <param name="BarcodeValue">Printed barcode identifier, when assigned.</param>
public sealed record AssetSnapshot(
    int AssetId,
    string AssetNumber,
    int? CurrentEmployeeId,
    int? CurrentLocationId,
    int? DepartmentId,
    string? CostCenter,
    bool IsBulk,
    string? AssetName = null,
    string? SerialNumber = null,
    decimal Quantity = 1m,
    string? ImportedBranch = null,
    string? ImportedLocation = null,
    string? QrCodeValue = null,
    string? BarcodeValue = null);
