namespace AMS.Modules.Organization.Features.CreateVendor;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateVendorMapper
{
    public static CreateVendorCommand ToCommand(CreateVendorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateVendorCommand(
            request.VendorName.Trim(),
            request.ContactPerson,
            request.Phone,
            request.Email);
    }
}
