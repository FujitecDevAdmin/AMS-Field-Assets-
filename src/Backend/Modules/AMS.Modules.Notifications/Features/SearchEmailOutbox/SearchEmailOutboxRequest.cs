namespace AMS.Modules.Notifications.Features.SearchEmailOutbox;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchEmailOutboxRequest(
    string? Status,
    string? SourceType,
    long? SourceId,
    string? Search,
    int? Skip,
    int? Take);
