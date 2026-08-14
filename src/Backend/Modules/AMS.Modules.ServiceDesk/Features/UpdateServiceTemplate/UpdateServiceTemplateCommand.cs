using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.UpdateServiceTemplate;

/// <summary>
/// Edit a template or retire it.
/// </summary>
public sealed record UpdateServiceTemplateCommand(
    int Id,
    string TemplateName,
    int? RequestCategoryId,
    int? RequestSubCategoryId,
    string DefaultPriority,
    int? DefaultSupportTeamId,
    string SubjectTemplate,
    string? DescriptionTemplate,
    bool RequiresAsset,
    int DisplayOrder,
    bool IsActive) : ICommand<UpdateServiceTemplateResponse>;
