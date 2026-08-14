namespace AMS.Modules.Allocations.Features.CreateCustomerSite;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateCustomerSiteMapper
{
    public static CreateCustomerSiteCommand ToCommand(CreateCustomerSiteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateCustomerSiteCommand(
            string.IsNullOrWhiteSpace(request.CustomerName) ? null : request.CustomerName.Trim(),
            request.SiteName.Trim(),
            string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            request.Latitude,
            request.Longitude);
    }
}
