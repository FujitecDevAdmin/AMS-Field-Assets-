using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Contracts.Features.RenewContract;

/// <summary>
/// Extend a contract to a new end date. Catalogue: Contract Detail.
/// </summary>
public sealed record RenewContractCommand(
    int Id,
    DateOnly NewEndDate,
    decimal? ContractValue,
    string? Remarks) : ICommand<RenewContractResponse>;
