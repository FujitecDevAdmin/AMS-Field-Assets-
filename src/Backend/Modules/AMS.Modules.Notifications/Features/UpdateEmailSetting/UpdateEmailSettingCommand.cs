using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Notifications.Features.UpdateEmailSetting;

/// <summary>
/// Edit an SMTP profile or retire it. Catalogue: E-mail Settings.
/// </summary>
public sealed record UpdateEmailSettingCommand(
    int Id,
    string ProfileName,
    string Host,
    int Port,
    bool UseSsl,
    string FromAddress,
    string? Username,
    string? Password,
    bool IsDefault,
    bool IsActive) : ICommand<UpdateEmailSettingResponse>;
