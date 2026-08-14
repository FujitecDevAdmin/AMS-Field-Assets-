namespace AMS.Modules.Assets.Features.SearchAssetTypes;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchAssetTypesRequest(
    bool? IsActive);
