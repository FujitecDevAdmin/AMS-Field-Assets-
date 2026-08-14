namespace AMS.Modules.Assets.Features.DefineCustomField;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record DefineCustomFieldRequest(
    string FieldName,
    string DisplayLabel,
    string FieldType,
    bool IsRequired,
    decimal? MinValue,
    decimal? MaxValue,
    string? ValidationRegex,
    string? DefaultValue,
    int? DisplayOrder,
    IReadOnlyList<string>? Options);
