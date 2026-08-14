namespace AMS.Modules.Organization.Features.CreateLocation;

/// <summary>
/// The new branch.
/// </summary>
/// <param name="Id">The new branch.</param>
/// <param name="LocationCode">Unique, upper-cased.</param>
/// <param name="LocationName">As stored, trimmed.</param>
/// <param name="IsHeadOffice">At most one branch in the whole system has this.</param>
public sealed record CreateLocationResponse(
    int Id,
    string LocationCode,
    string LocationName,
    bool IsHeadOffice);
