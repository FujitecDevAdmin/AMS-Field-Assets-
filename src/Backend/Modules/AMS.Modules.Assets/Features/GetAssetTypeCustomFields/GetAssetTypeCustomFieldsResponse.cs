namespace AMS.Modules.Assets.Features.GetAssetTypeCustomFields;

/// <summary>
/// What the asset form must render for this type.
/// </summary>
/// <param name="AssetTypeId">The type asked about.</param>
/// <param name="Rows">Its fields, in display order.</param>
public sealed record GetAssetTypeCustomFieldsResponse(
    int AssetTypeId,
    IReadOnlyList<GetAssetTypeCustomFieldsResponse.Row> Rows)
{
    /// <summary>One custom field, and everything the form needs to render it.</summary>
    /// <param name="Id">The field definition.</param>
    /// <param name="FieldName">Unique within the asset type. The key a value is stored against.</param>
    /// <param name="DisplayLabel">What the form shows beside the editor.</param>
    /// <param name="FieldType">Text, Number, Percentage, Date, Boolean or Dropdown (R2-26).</param>
    /// <param name="IsRequired">Whether the form may be submitted without it.</param>
    /// <param name="MinValue">Lower bound for Number and Percentage. Null means unbounded.</param>
    /// <param name="MaxValue">Upper bound. CK_CustomFieldDefinition_Range keeps it above MinValue.</param>
    /// <param name="ValidationRegex">Applied to Text. Null means no pattern.</param>
    /// <param name="DefaultValue">Pre-filled on a new asset. Null means blank.</param>
    /// <param name="DisplayOrder">The order the form lays them out in.</param>
    /// <param name="IsActive">Retired fields keep the values already captured against them.</param>
    /// <param name="Options">The dropdown values in display order; empty for every other type.</param>
    public sealed record Row(
        int Id,
        string FieldName,
        string DisplayLabel,
        string FieldType,
        bool IsRequired,
        decimal? MinValue,
        decimal? MaxValue,
        string? ValidationRegex,
        string? DefaultValue,
        int DisplayOrder,
        bool IsActive,
        IReadOnlyList<string> Options);
}
