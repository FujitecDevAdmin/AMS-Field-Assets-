namespace AMS.Modules.Assets.Features.CreateAssetType;

/// <summary>
/// The new type.
/// </summary>
/// <param name="Id">The new type.</param>
/// <param name="TypeName">Unique, trimmed.</param>
public sealed record CreateAssetTypeResponse(
    int Id,
    string TypeName);
