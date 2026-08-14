namespace AMS.Modules.Assets.Features.UpdateAssetStatus;

/// <summary>
/// The updated status.
/// </summary>
/// <param name="Id">The status edited.</param>
/// <param name="StatusName">Unique, trimmed.</param>
/// <param name="IsActive">Retiring is deactivation: assets currently in this status keep it.</param>
public sealed record UpdateAssetStatusResponse(
    int Id,
    string StatusName,
    bool IsActive);
