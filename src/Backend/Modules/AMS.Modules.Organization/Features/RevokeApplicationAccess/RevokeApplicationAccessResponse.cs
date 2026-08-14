namespace AMS.Modules.Organization.Features.RevokeApplicationAccess;

/// <summary>
/// The revoked grant.
/// </summary>
/// <param name="Id">The grant row, which stays: it is the record that access WAS held.</param>
/// <param name="RevokedOnUtc">When it was withdrawn. UTC.</param>
public sealed record RevokeApplicationAccessResponse(
    int Id,
    DateTime RevokedOnUtc);
