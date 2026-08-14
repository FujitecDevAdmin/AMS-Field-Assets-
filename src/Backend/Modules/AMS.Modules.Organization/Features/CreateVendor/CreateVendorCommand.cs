using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.CreateVendor;

/// <summary>
/// Add a vendor. Catalogue screen: Vendors.
/// </summary>
public sealed record CreateVendorCommand(
    string VendorName,
    string? ContactPerson,
    string? Phone,
    string? Email) : ICommand<CreateVendorResponse>;
