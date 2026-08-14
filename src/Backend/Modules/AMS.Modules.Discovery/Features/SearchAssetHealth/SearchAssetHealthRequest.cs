namespace AMS.Modules.Discovery.Features.SearchAssetHealth;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchAssetHealthRequest(
    int? AssetId,
    decimal? MinDrivePercent,
    int? NotSeenForHours,
    int? Skip,
    int? Take);
