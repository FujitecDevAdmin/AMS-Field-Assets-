using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Notifications.Features.SearchEmailSettings;

/// <summary>
/// The SMTP profiles. Catalogue: E-mail Settings.
/// </summary>
public sealed record SearchEmailSettingsQuery(
    bool ActiveOnly) : IQuery<SearchEmailSettingsResponse>;
