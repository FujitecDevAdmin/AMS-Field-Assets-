namespace AMS.Modules.Identity.Features.SignIn;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SignInMapper
{
    public static SignInCommand ToCommand(SignInRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SignInCommand(
            request.Username.Trim(),
            request.Password);
    }
}
