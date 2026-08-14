namespace AMS.Modules.Organization.Features.UpdateVendor;

/// <summary>
/// The updated vendor.
/// </summary>
/// <param name="Id">The vendor edited.</param>
/// <param name="VendorName">As stored, trimmed.</param>
/// <param name="IsActive">Retiring is deactivation, never deletion: rows elsewhere still point at this one.</param>
public sealed record UpdateVendorResponse(
    int Id,
    string VendorName,
    bool IsActive);
