namespace AMS.Modules.Verification.Features.CloseVerificationCycle;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CloseVerificationCycleMapper
{
    public static CloseVerificationCycleCommand ToCommand(CloseVerificationCycleRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CloseVerificationCycleCommand(
            id);
    }
}
