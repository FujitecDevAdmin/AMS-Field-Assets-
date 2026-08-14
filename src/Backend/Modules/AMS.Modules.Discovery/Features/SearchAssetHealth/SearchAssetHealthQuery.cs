using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Discovery.Features.SearchAssetHealth;

/// <summary>
/// How the machines are doing. Catalogue: Asset Health.
/// </summary>
public sealed record SearchAssetHealthQuery(
    int? AssetId,
    decimal? MinDrivePercent,
    int? NotSeenForHours,
    int Skip,
    int Take) : IQuery<SearchAssetHealthResponse>;
