namespace AMS.Modules.Organization.Features.CreateApplication;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateApplicationMapper
{
    public static CreateApplicationCommand ToCommand(CreateApplicationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateApplicationCommand(
            request.ApplicationName.Trim());
    }
}
