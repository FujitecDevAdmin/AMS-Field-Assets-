namespace AMS.Modules.Assets.Features.SearchAssetStatuses;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchAssetStatusesRequest(
    bool? IsActive);
