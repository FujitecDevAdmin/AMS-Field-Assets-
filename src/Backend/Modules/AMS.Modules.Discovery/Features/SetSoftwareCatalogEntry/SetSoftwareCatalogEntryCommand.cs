using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Discovery.Features.SetSoftwareCatalogEntry;

/// <summary>
/// Record what we are licensed for, or blacklist a title. Catalogue: Software Catalogue.
/// </summary>
public sealed record SetSoftwareCatalogEntryCommand(
    string SoftwareName,
    string? Publisher,
    int? LicensedSeats,
    int? ContractId,
    bool IsBlacklisted,
    bool IsActive) : ICommand<SetSoftwareCatalogEntryResponse>;
