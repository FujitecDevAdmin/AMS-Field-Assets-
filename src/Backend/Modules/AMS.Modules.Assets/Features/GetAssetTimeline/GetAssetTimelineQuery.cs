using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.GetAssetTimeline;

/// <summary>
/// Everything that has happened to one asset, newest first.
/// </summary>
public sealed record GetAssetTimelineQuery(
    int AssetId,
    int Skip,
    int Take) : IQuery<GetAssetTimelineResponse>;
