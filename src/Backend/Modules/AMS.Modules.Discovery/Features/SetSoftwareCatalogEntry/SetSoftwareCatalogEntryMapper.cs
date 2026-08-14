namespace AMS.Modules.Discovery.Features.SetSoftwareCatalogEntry;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SetSoftwareCatalogEntryMapper
{
    public static SetSoftwareCatalogEntryCommand ToCommand(SetSoftwareCatalogEntryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SetSoftwareCatalogEntryCommand(
            request.SoftwareName.Trim(),
            string.IsNullOrWhiteSpace(request.Publisher) ? null : request.Publisher.Trim(),
            request.LicensedSeats,
            request.ContractId,
            request.IsBlacklisted ?? false,
            request.IsActive ?? true);
    }
}
