using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.SetAssetCustomValues;

/// <summary>
/// Fill in the custom fields defined for this asset's type. Catalogue: Fill custom fields.
/// </summary>
/// <remarks>
/// The whole set at once, not one field per request. The form saves as a form,
/// and a field's value is only meaningful beside the others — a required field
/// left blank has to fail the save, not the field.
/// </remarks>
public sealed record SetAssetCustomValuesCommand(
    int AssetId,
    IReadOnlyList<SetAssetCustomValuesCommand.Entry> Values) : ICommand<SetAssetCustomValuesResponse>
{
    /// <summary>One field and what the user put in it.</summary>
    /// <param name="CustomFieldDefinitionId">Which field. Must belong to the asset's type.</param>
    /// <param name="Value">
    /// The raw text, for a Text or Dropdown field. Null or blank clears the
    /// value, which is how a form says "this was emptied".
    /// </param>
    /// <param name="ValueNumber">For Number and Percentage. Range-checked against the definition.</param>
    /// <param name="ValueDate">For Date.</param>
    /// <param name="OptionId">
    /// For Dropdown: the chosen <c>CustomFieldOption</c>. Stored as an id rather
    /// than as text so renaming an option does not silently rewrite history on
    /// every asset that chose it.
    /// </param>
    public sealed record Entry(
        int CustomFieldDefinitionId,
        string? Value,
        decimal? ValueNumber,
        DateOnly? ValueDate,
        int? OptionId);
}
