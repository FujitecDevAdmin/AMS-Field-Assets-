namespace AMS.Modules.Notifications.Features.UpdateEmailSetting;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateEmailSettingRequest(
    string ProfileName,
    string Host,
    int? Port,
    bool? UseSsl,
    string FromAddress,
    string? Username,
    string? Password,
    bool? IsDefault,
    bool? IsActive);
