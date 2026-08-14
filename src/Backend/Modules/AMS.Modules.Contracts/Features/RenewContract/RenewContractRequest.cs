namespace AMS.Modules.Contracts.Features.RenewContract;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record RenewContractRequest(
    DateOnly NewEndDate,
    decimal? ContractValue,
    string? Remarks);
