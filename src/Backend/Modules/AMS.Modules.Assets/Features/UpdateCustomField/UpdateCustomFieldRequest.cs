namespace AMS.Modules.Assets.Features.UpdateCustomField;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateCustomFieldRequest(
    string DisplayLabel,
    bool IsRequired,
    decimal? MinValue,
    decimal? MaxValue,
    string? ValidationRegex,
    string? DefaultValue,
    int? DisplayOrder,
    bool IsActive);
