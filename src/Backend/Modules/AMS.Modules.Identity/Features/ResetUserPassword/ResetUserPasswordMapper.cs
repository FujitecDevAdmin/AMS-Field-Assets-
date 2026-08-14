namespace AMS.Modules.Identity.Features.ResetUserPassword;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class ResetUserPasswordMapper
{
    public static ResetUserPasswordCommand ToCommand(ResetUserPasswordRequest request, int userId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ResetUserPasswordCommand(
            userId,
            request.NewPassword);
    }
}
