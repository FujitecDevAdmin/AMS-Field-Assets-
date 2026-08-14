namespace AMS.Modules.Notifications.Features.CreateEmailSetting;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateEmailSettingMapper
{
    public static CreateEmailSettingCommand ToCommand(CreateEmailSettingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateEmailSettingCommand(
            request.ProfileName.Trim(),
            request.Host.Trim(),
            request.Port ?? 25,
            request.UseSsl ?? true,
            request.FromAddress.Trim(),
            string.IsNullOrWhiteSpace(request.Username) ? null : request.Username.Trim(),
            string.IsNullOrEmpty(request.Password) ? null : request.Password,
            request.IsDefault ?? false);
    }
}
