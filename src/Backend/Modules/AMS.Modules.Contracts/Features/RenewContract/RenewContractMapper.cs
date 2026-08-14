namespace AMS.Modules.Contracts.Features.RenewContract;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class RenewContractMapper
{
    public static RenewContractCommand ToCommand(RenewContractRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RenewContractCommand(
            id,
            request.NewEndDate,
            request.ContractValue,
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim());
    }
}
