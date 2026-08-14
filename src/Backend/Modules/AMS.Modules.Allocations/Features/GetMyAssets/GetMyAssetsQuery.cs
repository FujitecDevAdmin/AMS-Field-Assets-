using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.GetMyAssets;

/// <summary>
/// What the signed-in employee holds. Catalogue screen: My Assets.
/// </summary>
public sealed record GetMyAssetsQuery(
) : IQuery<GetMyAssetsResponse>;
