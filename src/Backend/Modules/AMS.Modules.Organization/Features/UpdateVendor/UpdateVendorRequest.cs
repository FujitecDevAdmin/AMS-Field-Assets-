namespace AMS.Modules.Organization.Features.UpdateVendor;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateVendorRequest(
    string VendorName,
    string? ContactPerson,
    string? Phone,
    string? Email,
    bool IsActive);
