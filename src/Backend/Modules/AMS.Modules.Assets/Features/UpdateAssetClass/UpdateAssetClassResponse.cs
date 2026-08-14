namespace AMS.Modules.Assets.Features.UpdateAssetClass;

/// <summary>
/// The updated class.
/// </summary>
/// <param name="Id">The class edited.</param>
/// <param name="ClassCode">Unique.</param>
/// <param name="IsActive">Retiring is deactivation: assets already classified keep pointing here.</param>
public sealed record UpdateAssetClassResponse(
    int Id,
    string ClassCode,
    bool IsActive);
