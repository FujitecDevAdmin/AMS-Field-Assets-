using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Contracts.Features.UpdateContract;

/// <summary>
/// Edit a contract or retire it. Catalogue: Contract Detail.
/// </summary>
public sealed record UpdateContractCommand(
    int Id,
    string ContractName,
    int? VendorId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? ContractValue,
    int? LicensedSeats,
    string? LicenceKey,
    bool AutoRenew,
    string? Remarks,
    bool IsDeleted) : ICommand<UpdateContractResponse>;
