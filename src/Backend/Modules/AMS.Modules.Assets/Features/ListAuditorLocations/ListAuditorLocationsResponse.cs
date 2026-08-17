namespace AMS.Modules.Assets.Features.ListAuditorLocations;

public sealed record ListAuditorLocationsResponse(
    IReadOnlyList<ListAuditorLocationsResponse.Row> Rows)
{
    public sealed record Row(int Id, int LocationId, string LocationName);
}
