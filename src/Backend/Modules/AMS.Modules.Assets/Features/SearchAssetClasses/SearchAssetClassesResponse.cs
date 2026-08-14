namespace AMS.Modules.Assets.Features.SearchAssetClasses;

/// <summary>
/// Every class with the reporting category it rolls up to.
/// </summary>
/// <param name="Rows">The classes, in code order.</param>
public sealed record SearchAssetClassesResponse(
    IReadOnlyList<SearchAssetClassesResponse.Row> Rows)
{
    /// <summary>One asset class.</summary>
    /// <param name="Id">The class.</param>
    /// <param name="ClassCode">Unique, enforced by UX_AssetClass_Code. The importer matches on it.</param>
    /// <param name="ClassName">Unique, enforced by UX_AssetClass_Name.</param>
    /// <param name="ReportingCategory">
    /// What the class rolls up to on a report. A column and not a table because it is a pure
    /// function of the class — five classes report as Plant &amp; Machinery.
    /// </param>
    /// <param name="IsDepreciable">Leasehold land is not.</param>
    /// <param name="IsIntangible">Software and similar.</param>
    /// <param name="IsAuc">
    /// Assets under construction. Exactly one class carries this, and
    /// UX_AssetClass_OneAuc keeps it that way, because the capitalisation step
    /// finds its source class by this flag.
    /// </param>
    /// <param name="IsActive">Retired classes stay, because assets still point at them.</param>
    /// <param name="AssetCount">Assets in this class, excluding deleted ones.</param>
    public sealed record Row(
        int Id,
        string ClassCode,
        string ClassName,
        string ReportingCategory,
        bool IsDepreciable,
        bool IsIntangible,
        bool IsAuc,
        bool IsActive,
        int AssetCount);
}
