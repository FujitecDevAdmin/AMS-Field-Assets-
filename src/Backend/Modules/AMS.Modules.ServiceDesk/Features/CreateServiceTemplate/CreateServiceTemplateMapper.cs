namespace AMS.Modules.ServiceDesk.Features.CreateServiceTemplate;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateServiceTemplateMapper
{
    public static CreateServiceTemplateCommand ToCommand(CreateServiceTemplateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateServiceTemplateCommand(
            request.TemplateName.Trim(),
            request.RequestKind.Trim(),
            request.RequestCategoryId,
            request.RequestSubCategoryId,
            request.DefaultPriority.Trim(),
            request.DefaultSupportTeamId,
            request.SubjectTemplate.Trim(),
            string.IsNullOrWhiteSpace(request.DescriptionTemplate) ? null : request.DescriptionTemplate.Trim(),
            request.RequiresAsset ?? false,
            request.DisplayOrder ?? 0);
    }
}
