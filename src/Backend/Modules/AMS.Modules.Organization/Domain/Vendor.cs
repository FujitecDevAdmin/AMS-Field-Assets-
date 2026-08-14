namespace AMS.Modules.Organization.Domain;

/// <summary>
/// Mirrors <c>[Organization].[Vendor]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class Vendor
{
    public int Id { get; set; }

    public required string VendorName { get; set; }

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
