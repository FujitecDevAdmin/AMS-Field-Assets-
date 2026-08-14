namespace AMS.Modules.Organization.Features.UpdateVendor;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateVendorMapper
{
    public static UpdateVendorCommand ToCommand(UpdateVendorRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateVendorCommand(
            id,
            request.VendorName.Trim(),
            request.ContactPerson,
            request.Phone,
            request.Email,
            request.IsActive);
    }
}
