namespace AMS.Modules.Notifications.Features.UpdateEmailSetting;

/// <summary>
/// The profile as it now stands.
/// </summary>
/// <param name="Id">The profile.</param>
/// <param name="ProfileName">What it is called.</param>
/// <param name="IsActive">Whether the dispatcher may use it.</param>
public sealed record UpdateEmailSettingResponse(
    int Id,
    string ProfileName,
    bool IsActive);
