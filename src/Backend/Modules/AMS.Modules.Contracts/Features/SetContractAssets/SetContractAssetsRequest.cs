namespace AMS.Modules.Contracts.Features.SetContractAssets;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SetContractAssetsRequest(
    IReadOnlyList<int> AssetIds);
