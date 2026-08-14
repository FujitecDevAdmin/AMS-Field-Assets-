using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.MapAssetToSite;

/// <summary>
/// Put an asset at a customer site. Catalogue: Map an asset to a customer site — for equipment installed at a customer rather than held by a person.
/// </summary>
public sealed record MapAssetToSiteCommand(
    int CustomerSiteId,
    int AssetId,
    DateOnly? CommissionedDate) : ICommand<MapAssetToSiteResponse>;
