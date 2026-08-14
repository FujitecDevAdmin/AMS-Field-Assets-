namespace AMS.Modules.ServiceDesk.Features.UpdateServiceTemplate;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateServiceTemplateMapper
{
    public static UpdateServiceTemplateCommand ToCommand(UpdateServiceTemplateRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateServiceTemplateCommand(
            id,
            request.TemplateName.Trim(),
            request.RequestCategoryId,
            request.RequestSubCategoryId,
            request.DefaultPriority.Trim(),
            request.DefaultSupportTeamId,
            request.SubjectTemplate.Trim(),
            string.IsNullOrWhiteSpace(request.DescriptionTemplate) ? null : request.DescriptionTemplate.Trim(),
            request.RequiresAsset ?? false,
            request.DisplayOrder ?? 0,
            request.IsActive);
    }
}
