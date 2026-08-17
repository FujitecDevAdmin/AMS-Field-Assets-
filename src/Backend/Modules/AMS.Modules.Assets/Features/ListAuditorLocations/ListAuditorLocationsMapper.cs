namespace AMS.Modules.Assets.Features.ListAuditorLocations;

public static class ListAuditorLocationsMapper
{
    public static ListAuditorLocationsQuery ToQuery(ListAuditorLocationsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ListAuditorLocationsQuery();
    }
}
