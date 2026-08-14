namespace AMS.Modules.Verification.Features.OpenVerificationCycle;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class OpenVerificationCycleMapper
{
    public static OpenVerificationCycleCommand ToCommand(OpenVerificationCycleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new OpenVerificationCycleCommand(
            request.CycleName.Trim(),
            request.StartDate ?? default,
            request.EndDate);
    }
}
