namespace AMS.Modules.ServiceDesk.Features.UpdateServiceTemplate;

/// <summary>
/// The updated template.
/// </summary>
/// <param name="Id">The template.</param>
/// <param name="TemplateName">Unique, trimmed.</param>
/// <param name="IsActive">Retiring hides it from the raise screen. RequestKind is not editable.</param>
public sealed record UpdateServiceTemplateResponse(
    int Id,
    string TemplateName,
    bool IsActive);
