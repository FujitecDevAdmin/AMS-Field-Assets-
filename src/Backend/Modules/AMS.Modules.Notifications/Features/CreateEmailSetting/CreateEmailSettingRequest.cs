namespace AMS.Modules.Notifications.Features.CreateEmailSetting;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateEmailSettingRequest(
    string ProfileName,
    string Host,
    int? Port,
    bool? UseSsl,
    string FromAddress,
    string? Username,
    string? Password,
    bool? IsDefault);
