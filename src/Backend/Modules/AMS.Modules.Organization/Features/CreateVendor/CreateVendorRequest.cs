namespace AMS.Modules.Organization.Features.CreateVendor;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateVendorRequest(
    string VendorName,
    string? ContactPerson,
    string? Phone,
    string? Email);
