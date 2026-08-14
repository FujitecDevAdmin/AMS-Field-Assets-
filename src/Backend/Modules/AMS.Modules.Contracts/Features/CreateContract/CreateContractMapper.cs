namespace AMS.Modules.Contracts.Features.CreateContract;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateContractMapper
{
    public static CreateContractCommand ToCommand(CreateContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateContractCommand(
            request.ContractNumber.Trim(),
            request.ContractName.Trim(),
            request.ContractType.Trim(),
            request.VendorId,
            request.StartDate,
            request.EndDate,
            request.ContractValue,
            request.LicensedSeats,
            string.IsNullOrEmpty(request.LicenceKey) ? null : request.LicenceKey,
            request.AutoRenew ?? false,
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
            request.AssetIds ?? []);
    }
}
