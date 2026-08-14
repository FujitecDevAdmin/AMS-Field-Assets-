namespace AMS.Modules.Contracts.Features.UpdateContract;

/// <summary>
/// The contract as it now stands.
/// </summary>
/// <param name="Id">The contract.</param>
/// <param name="ContractNumber">Unchanged: the number is how it is quoted.</param>
/// <param name="IsDeleted">Whether it has been retired. Nothing is removed.</param>
public sealed record UpdateContractResponse(
    int Id,
    string ContractNumber,
    bool IsDeleted);
