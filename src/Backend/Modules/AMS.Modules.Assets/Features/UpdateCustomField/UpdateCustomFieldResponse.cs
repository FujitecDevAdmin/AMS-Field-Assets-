namespace AMS.Modules.Assets.Features.UpdateCustomField;

/// <summary>
/// The updated field definition.
/// </summary>
/// <param name="Id">The field edited.</param>
/// <param name="DisplayLabel">What the form shows beside the editor.</param>
/// <param name="IsActive">Retiring hides the field from new assets; values already captured stay.</param>
public sealed record UpdateCustomFieldResponse(
    int Id,
    string DisplayLabel,
    bool IsActive);
