using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.UpdateVendor;

/// <summary>
/// Rename a vendor or retire it. Catalogue screen: Vendors.
/// </summary>
public sealed record UpdateVendorCommand(
    int Id,
    string VendorName,
    string? ContactPerson,
    string? Phone,
    string? Email,
    bool IsActive) : ICommand<UpdateVendorResponse>;
