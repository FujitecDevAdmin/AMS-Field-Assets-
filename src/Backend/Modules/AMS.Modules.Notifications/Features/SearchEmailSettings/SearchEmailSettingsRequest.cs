namespace AMS.Modules.Notifications.Features.SearchEmailSettings;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchEmailSettingsRequest(
    bool? ActiveOnly);
