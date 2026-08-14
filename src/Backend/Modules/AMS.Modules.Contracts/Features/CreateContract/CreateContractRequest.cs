namespace AMS.Modules.Contracts.Features.CreateContract;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateContractRequest(
    string ContractNumber,
    string ContractName,
    string ContractType,
    int? VendorId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? ContractValue,
    int? LicensedSeats,
    string? LicenceKey,
    bool? AutoRenew,
    string? Remarks,
    IReadOnlyList<int>? AssetIds);
