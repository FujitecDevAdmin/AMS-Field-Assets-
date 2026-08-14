namespace AMS.Modules.Assets.Features.GetAssetTypeCustomFields;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record GetAssetTypeCustomFieldsRequest(
    int AssetTypeId,
    bool? IncludeInactive);
