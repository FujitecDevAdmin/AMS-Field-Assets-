namespace AMS.Modules.Organization.Features.SearchVendors;

/// <summary>
/// Every vendor matching the filter. Not paged: these tables hold tens of rows.
/// </summary>
/// <param name="Rows">The vendors.</param>
public sealed record SearchVendorsResponse(IReadOnlyList<SearchVendorsResponse.Row> Rows)
{
    /// <summary>One vendor.</summary>
    /// <param name="Id">The vendor.</param>
    /// <param name="VendorName">Unique, enforced by UX_Vendor_Name.</param>
    /// <param name="ContactPerson">May be null.</param>
    /// <param name="Phone">May be null.</param>
    /// <param name="Email">May be null.</param>
    /// <param name="IsActive">Retired vendors stay, because purchases and contracts point at them.</param>
    public sealed record Row(
        int Id,
        string VendorName,
        string? ContactPerson,
        string? Phone,
        string? Email,
        bool IsActive);
}
