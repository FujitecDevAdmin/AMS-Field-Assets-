namespace AMS.Modules.Identity.Features.ConfirmMfaEnrolment;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class ConfirmMfaEnrolmentMapper
{
    public static ConfirmMfaEnrolmentCommand ToCommand(ConfirmMfaEnrolmentRequest request, AMS.SharedKernel.Abstractions.ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ConfirmMfaEnrolmentCommand(
            currentUser.Id,
            request.Code.Trim());
    }
}
