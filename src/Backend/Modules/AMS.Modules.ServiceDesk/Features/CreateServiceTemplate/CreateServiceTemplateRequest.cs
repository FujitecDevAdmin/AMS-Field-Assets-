namespace AMS.Modules.ServiceDesk.Features.CreateServiceTemplate;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateServiceTemplateRequest(
    string TemplateName,
    string RequestKind,
    int? RequestCategoryId,
    int? RequestSubCategoryId,
    string DefaultPriority,
    int? DefaultSupportTeamId,
    string SubjectTemplate,
    string? DescriptionTemplate,
    bool? RequiresAsset,
    int? DisplayOrder);
