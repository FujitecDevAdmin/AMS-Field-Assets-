using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Notifications.Features.CreateEmailSetting;

/// <summary>
/// Add an SMTP profile. Catalogue: E-mail Settings.
/// </summary>
public sealed record CreateEmailSettingCommand(
    string ProfileName,
    string Host,
    int Port,
    bool UseSsl,
    string FromAddress,
    string? Username,
    string? Password,
    bool IsDefault) : ICommand<CreateEmailSettingResponse>;
