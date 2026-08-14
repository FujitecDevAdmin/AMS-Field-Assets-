namespace AMS.Modules.Allocations.Features.UpdateCustomerSite;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateCustomerSiteMapper
{
    public static UpdateCustomerSiteCommand ToCommand(UpdateCustomerSiteRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateCustomerSiteCommand(
            id,
            string.IsNullOrWhiteSpace(request.CustomerName) ? null : request.CustomerName.Trim(),
            request.SiteName.Trim(),
            string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            request.Latitude,
            request.Longitude,
            request.IsActive);
    }
}
