using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.SearchAssetStatuses;

/// <summary>
/// The asset status lookup. Catalogue screen: Asset Statuses.
/// </summary>
public sealed record SearchAssetStatusesQuery(
    bool? IsActive) : IQuery<SearchAssetStatusesResponse>;
