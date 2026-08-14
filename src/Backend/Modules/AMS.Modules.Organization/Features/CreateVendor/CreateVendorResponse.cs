namespace AMS.Modules.Organization.Features.CreateVendor;

/// <summary>
/// The new vendor.
/// </summary>
/// <param name="Id">The new vendor.</param>
/// <param name="VendorName">As stored, trimmed.</param>
public sealed record CreateVendorResponse(
    int Id,
    string VendorName);
