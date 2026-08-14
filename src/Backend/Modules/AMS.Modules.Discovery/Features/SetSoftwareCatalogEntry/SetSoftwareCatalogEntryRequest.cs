namespace AMS.Modules.Discovery.Features.SetSoftwareCatalogEntry;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SetSoftwareCatalogEntryRequest(
    string SoftwareName,
    string? Publisher,
    int? LicensedSeats,
    int? ContractId,
    bool? IsBlacklisted,
    bool? IsActive);
