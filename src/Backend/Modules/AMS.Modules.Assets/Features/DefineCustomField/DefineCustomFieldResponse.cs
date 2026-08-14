namespace AMS.Modules.Assets.Features.DefineCustomField;

/// <summary>
/// The new field definition.
/// </summary>
/// <param name="Id">The new field.</param>
/// <param name="FieldName">Unique within the asset type.</param>
/// <param name="FieldType">One of Text, Number, Percentage, Date, Boolean, Dropdown (R2-26).</param>
/// <param name="Options">The dropdown values, empty for every other type.</param>
public sealed record DefineCustomFieldResponse(
    int Id,
    string FieldName,
    string FieldType,
    IReadOnlyList<string> Options);
