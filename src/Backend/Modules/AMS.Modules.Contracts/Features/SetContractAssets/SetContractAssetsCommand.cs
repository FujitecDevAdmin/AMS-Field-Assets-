using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Contracts.Features.SetContractAssets;

/// <summary>
/// Say what a contract covers. Catalogue: Contract Detail.
/// </summary>
public sealed record SetContractAssetsCommand(
    int Id,
    IReadOnlyList<int> AssetIds) : ICommand<SetContractAssetsResponse>;
