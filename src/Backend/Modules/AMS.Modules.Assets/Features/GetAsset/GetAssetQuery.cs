using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.GetAsset;

/// <summary>
/// One asset in full. Catalogue screen: Asset Detail and Timeline.
/// </summary>
public sealed record GetAssetQuery(
    int Id) : IQuery<GetAssetResponse>;
