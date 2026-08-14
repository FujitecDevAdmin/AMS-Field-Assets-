namespace AMS.Modules.Contracts.Features.UpdateContract;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateContractMapper
{
    public static UpdateContractCommand ToCommand(UpdateContractRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateContractCommand(
            id,
            request.ContractName.Trim(),
            request.VendorId,
            request.StartDate,
            request.EndDate,
            request.ContractValue,
            request.LicensedSeats,
            string.IsNullOrEmpty(request.LicenceKey) ? null : request.LicenceKey,
            request.AutoRenew ?? false,
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
            request.IsDeleted ?? false);
    }
}
