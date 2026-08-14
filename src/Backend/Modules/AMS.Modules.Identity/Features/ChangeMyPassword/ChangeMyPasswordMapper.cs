namespace AMS.Modules.Identity.Features.ChangeMyPassword;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class ChangeMyPasswordMapper
{
    public static ChangeMyPasswordCommand ToCommand(ChangeMyPasswordRequest request, AMS.SharedKernel.Abstractions.ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ChangeMyPasswordCommand(
            currentUser.Id,
            request.CurrentPassword,
            request.NewPassword);
    }
}
