namespace AMS.Modules.Assets.Features.DefineCustomField;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class DefineCustomFieldMapper
{
    public static DefineCustomFieldCommand ToCommand(DefineCustomFieldRequest request, int assetTypeId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new DefineCustomFieldCommand(
            assetTypeId,
            request.FieldName.Trim(),
            request.DisplayLabel.Trim(),
            request.FieldType.Trim(),
            request.IsRequired,
            request.MinValue,
            request.MaxValue,
            string.IsNullOrWhiteSpace(request.ValidationRegex) ? null : request.ValidationRegex.Trim(),
            string.IsNullOrWhiteSpace(request.DefaultValue) ? null : request.DefaultValue.Trim(),
            request.DisplayOrder ?? 0,
            request.Options ?? []);
    }
}
