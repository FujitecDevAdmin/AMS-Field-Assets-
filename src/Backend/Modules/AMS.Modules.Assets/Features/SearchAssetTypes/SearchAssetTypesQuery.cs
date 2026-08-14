using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.SearchAssetTypes;

/// <summary>
/// The asset type tree. Catalogue screen: Asset Types and Custom Fields.
/// </summary>
public sealed record SearchAssetTypesQuery(
    bool? IsActive) : IQuery<SearchAssetTypesResponse>;
