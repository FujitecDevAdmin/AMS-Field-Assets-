namespace AMS.Modules.Contracts.Features.CreateContract;

/// <summary>
/// The contract, live.
/// </summary>
/// <param name="Id">The contract.</param>
/// <param name="ContractNumber">What it is quoted by.</param>
/// <param name="AssetCount">How many assets it covers.</param>
public sealed record CreateContractResponse(
    int Id,
    string ContractNumber,
    int AssetCount);
