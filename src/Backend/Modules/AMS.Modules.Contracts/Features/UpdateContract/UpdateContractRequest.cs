namespace AMS.Modules.Contracts.Features.UpdateContract;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateContractRequest(
    string ContractName,
    int? VendorId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? ContractValue,
    int? LicensedSeats,
    string? LicenceKey,
    bool? AutoRenew,
    string? Remarks,
    bool? IsDeleted);
