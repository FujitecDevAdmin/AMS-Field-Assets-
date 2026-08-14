namespace AMS.Modules.Identity.Features.GetUserCapabilities;

/// <summary>Request to query. Explicit, like every mapper (docs/02 §4).</summary>
public static class GetUserCapabilitiesMapper
{
    public static GetUserCapabilitiesQuery ToQuery(GetUserCapabilitiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new GetUserCapabilitiesQuery(request.UserId);
    }
}
