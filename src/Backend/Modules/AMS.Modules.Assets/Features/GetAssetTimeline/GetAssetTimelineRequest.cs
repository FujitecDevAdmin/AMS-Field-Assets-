namespace AMS.Modules.Assets.Features.GetAssetTimeline;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record GetAssetTimelineRequest(
    int AssetId,
    int? Skip,
    int? Take);
