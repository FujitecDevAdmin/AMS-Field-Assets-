namespace AMS.Modules.Notifications.Features.CreateEmailSetting;

/// <summary>
/// The profile, live.
/// </summary>
/// <param name="Id">The profile.</param>
/// <param name="ProfileName">What it is called.</param>
/// <param name="IsDefault">Whether the dispatcher sends through it. At most one profile may.</param>
public sealed record CreateEmailSettingResponse(
    int Id,
    string ProfileName,
    bool IsDefault);
