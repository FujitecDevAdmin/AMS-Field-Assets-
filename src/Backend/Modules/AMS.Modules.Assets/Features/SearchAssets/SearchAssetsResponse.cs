namespace AMS.Modules.Assets.Features.SearchAssets;

/// <summary>
/// One page of the register, and how many match in total.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">Assets matching the filter, ignoring paging.</param>
public sealed record SearchAssetsResponse(
    IReadOnlyList<SearchAssetsResponse.Row> Rows,
    int TotalCount)
{
    /// <summary>One line of the register grid.</summary>
    /// <param name="Id">The asset.</param>
    /// <param name="AssetNumber">Unique, enforced by UX_Asset_Number.</param>
    /// <param name="AssetName">As stored.</param>
    /// <param name="SerialNumber">Null on bulk lines and on anything without one.</param>
    /// <param name="TypeName">What the thing IS — the operational axis.</param>
    /// <param name="ClassName">
    /// What the accounts call it. Null until somebody classifies it, which is
    /// normal for an asset keyed before the finance import runs.
    /// </param>
    /// <param name="StatusName">Where it is in its life.</param>
    /// <param name="Make">Promoted onto the asset in Revision 3: a chair has a make too.</param>
    /// <param name="Model">As above.</param>
    /// <param name="CurrentLocationId">
    /// The branch holding it, <b>id only</b>. Null on a bulk line, which has no
    /// single location — its balances live in AssetHolding, one per place.
    ///
    /// An id and not a name because <c>[Organization]</c> is another module:
    /// rule 2 forbids the join and there is no read contract to ask through.
    /// The grid resolves it from the branch list it already loaded to populate
    /// its own filter, so this costs the client nothing.
    /// </param>
    /// <param name="CurrentEmployeeId">Who is accountable for it, if anybody. Id only, as above.</param>
    /// <param name="DepartmentId">The owning department id, when assigned.</param>
    /// <param name="CostCenter">Finance cost-centre code.</param>
    /// <param name="QrCodeValue">Value encoded in the asset QR label.</param>
    /// <param name="BarcodeValue">Value printed as the asset barcode.</param>
    /// <param name="ErpAssetNumber">Asset reference in the source ERP.</param>
    /// <param name="SapAssetNumber">SAP fixed-asset number.</param>
    /// <param name="SapPlant">SAP plant code.</param>
    /// <param name="LastPhysicalCheckOnUtc">Most recent physical verification time.</param>
    /// <param name="Remarks">Register remarks.</param>
    /// <param name="ImportedDataJson">Original 70-column import row for drill-down.</param>
    /// <param name="IsBulk">Whether this line is counted rather than issued.</param>
    /// <param name="Quantity">Always 1 for a unit asset — CK_Asset_UnitQuantityIsOne.</param>
    /// <param name="UnitOfMeasure">Nos, Set, Metre. Null unless the line is bulk.</param>
    /// <param name="AcquisitionDate">When it was acquired, if known.</param>
    /// <param name="IsDeleted">
    /// Deleted assets are hidden by default and returned only when explicitly
    /// asked for, because history points at them.
    /// </param>
    public sealed record Row(
        int Id,
        string AssetNumber,
        string AssetName,
        string? SerialNumber,
        string TypeName,
        string? ClassName,
        string StatusName,
        string? Make,
        string? Model,
        int? CurrentLocationId,
        int? CurrentEmployeeId,
        int? DepartmentId,
        string? CostCenter,
        string? QrCodeValue,
        string? BarcodeValue,
        string? ErpAssetNumber,
        string? SapAssetNumber,
        string? SapPlant,
        DateTime? LastPhysicalCheckOnUtc,
        string? Remarks,
        string? ImportedDataJson,
        bool IsBulk,
        decimal Quantity,
        string? UnitOfMeasure,
        DateOnly? AcquisitionDate,
        bool IsDeleted);
}
