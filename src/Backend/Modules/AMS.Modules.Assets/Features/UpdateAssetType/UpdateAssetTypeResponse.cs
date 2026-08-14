namespace AMS.Modules.Assets.Features.UpdateAssetType;

/// <summary>
/// The updated type.
/// </summary>
/// <param name="Id">The type edited.</param>
/// <param name="TypeName">Unique, trimmed.</param>
/// <param name="IsActive">Retiring is deactivation: assets and custom fields point here.</param>
public sealed record UpdateAssetTypeResponse(
    int Id,
    string TypeName,
    bool IsActive);
