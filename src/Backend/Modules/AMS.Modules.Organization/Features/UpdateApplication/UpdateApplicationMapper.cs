namespace AMS.Modules.Organization.Features.UpdateApplication;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateApplicationMapper
{
    public static UpdateApplicationCommand ToCommand(UpdateApplicationRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateApplicationCommand(
            id,
            request.ApplicationName.Trim(),
            request.IsActive);
    }
}
