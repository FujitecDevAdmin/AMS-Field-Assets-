namespace AMS.Modules.Identity.Features.RegenerateRecoveryCodes;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class RegenerateRecoveryCodesMapper
{
    public static RegenerateRecoveryCodesCommand ToCommand(RegenerateRecoveryCodesRequest request, AMS.SharedKernel.Abstractions.ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RegenerateRecoveryCodesCommand(
            currentUser.Id,
            request.Code.Trim());
    }
}
