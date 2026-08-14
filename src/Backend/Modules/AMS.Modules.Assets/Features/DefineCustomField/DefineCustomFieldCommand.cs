using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.DefineCustomField;

/// <summary>
/// Add a custom field to an asset type. Catalogue: Define custom fields - type, required flag, range and dropdown options.
/// </summary>
public sealed record DefineCustomFieldCommand(
    int AssetTypeId,
    string FieldName,
    string DisplayLabel,
    string FieldType,
    bool IsRequired,
    decimal? MinValue,
    decimal? MaxValue,
    string? ValidationRegex,
    string? DefaultValue,
    int DisplayOrder,
    IReadOnlyList<string> Options) : ICommand<DefineCustomFieldResponse>;
