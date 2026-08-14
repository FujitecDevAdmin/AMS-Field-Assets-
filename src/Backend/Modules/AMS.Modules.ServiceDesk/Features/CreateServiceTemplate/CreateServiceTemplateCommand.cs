using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.CreateServiceTemplate;

/// <summary>
/// Add a template. Catalogue: pre-written requests with a default category, priority and team.
/// </summary>
public sealed record CreateServiceTemplateCommand(
    string TemplateName,
    string RequestKind,
    int? RequestCategoryId,
    int? RequestSubCategoryId,
    string DefaultPriority,
    int? DefaultSupportTeamId,
    string SubjectTemplate,
    string? DescriptionTemplate,
    bool RequiresAsset,
    int DisplayOrder) : ICommand<CreateServiceTemplateResponse>;
