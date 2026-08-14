namespace AMS.Modules.Assets.Features.UpdateCustomField;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateCustomFieldMapper
{
    public static UpdateCustomFieldCommand ToCommand(UpdateCustomFieldRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateCustomFieldCommand(
            id,
            request.DisplayLabel.Trim(),
            request.IsRequired,
            request.MinValue,
            request.MaxValue,
            string.IsNullOrWhiteSpace(request.ValidationRegex) ? null : request.ValidationRegex.Trim(),
            string.IsNullOrWhiteSpace(request.DefaultValue) ? null : request.DefaultValue.Trim(),
            request.DisplayOrder ?? 0,
            request.IsActive);
    }
}
