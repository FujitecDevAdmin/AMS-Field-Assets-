namespace AMS.Modules.Organization.Features.UpdateLocation;

/// <summary>
/// The updated branch.
/// </summary>
/// <param name="Id">The branch edited.</param>
/// <param name="LocationCode">Unique, upper-cased.</param>
/// <param name="IsHeadOffice">At most one across the system.</param>
/// <param name="IsActive">Retiring is deactivation; assets and employees still point here.</param>
public sealed record UpdateLocationResponse(
    int Id,
    string LocationCode,
    bool IsHeadOffice,
    bool IsActive);
