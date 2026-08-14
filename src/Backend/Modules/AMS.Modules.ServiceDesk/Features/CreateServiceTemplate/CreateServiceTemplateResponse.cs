namespace AMS.Modules.ServiceDesk.Features.CreateServiceTemplate;

/// <summary>
/// The new template.
/// </summary>
/// <param name="Id">The template.</param>
/// <param name="TemplateName">Unique, trimmed.</param>
public sealed record CreateServiceTemplateResponse(
    int Id,
    string TemplateName);
