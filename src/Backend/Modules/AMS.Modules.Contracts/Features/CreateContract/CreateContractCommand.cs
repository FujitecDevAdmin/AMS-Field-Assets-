using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Contracts.Features.CreateContract;

/// <summary>
/// Record a contract. Catalogue: Contracts.
/// </summary>
public sealed record CreateContractCommand(
    string ContractNumber,
    string ContractName,
    string ContractType,
    int? VendorId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? ContractValue,
    int? LicensedSeats,
    string? LicenceKey,
    bool AutoRenew,
    string? Remarks,
    IReadOnlyList<int> AssetIds) : ICommand<CreateContractResponse>;
