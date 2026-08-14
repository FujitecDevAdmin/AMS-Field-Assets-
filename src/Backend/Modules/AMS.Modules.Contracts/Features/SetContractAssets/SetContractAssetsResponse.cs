namespace AMS.Modules.Contracts.Features.SetContractAssets;

/// <summary>
/// What it covers now.
/// </summary>
/// <param name="Id">The contract.</param>
/// <param name="AssetCount">How many assets are covered.</param>
public sealed record SetContractAssetsResponse(
    int Id,
    int AssetCount);
