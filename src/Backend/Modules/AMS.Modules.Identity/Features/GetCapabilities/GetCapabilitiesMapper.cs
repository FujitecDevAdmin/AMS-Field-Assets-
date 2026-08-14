namespace AMS.Modules.Identity.Features.GetCapabilities;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetCapabilitiesMapper
{
    public static GetCapabilitiesQuery ToQuery(GetCapabilitiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetCapabilitiesQuery(
            request.Module?.Trim());
    }
}
