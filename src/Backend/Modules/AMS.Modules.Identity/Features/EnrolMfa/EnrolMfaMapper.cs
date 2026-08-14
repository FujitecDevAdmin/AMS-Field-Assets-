namespace AMS.Modules.Identity.Features.EnrolMfa;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class EnrolMfaMapper
{
    public static EnrolMfaCommand ToCommand(EnrolMfaRequest request, AMS.SharedKernel.Abstractions.ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new EnrolMfaCommand(
            currentUser.Id);
    }
}
