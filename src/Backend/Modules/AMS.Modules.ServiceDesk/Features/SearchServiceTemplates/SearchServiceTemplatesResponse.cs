namespace AMS.Modules.ServiceDesk.Features.SearchServiceTemplates;

/// <summary>
/// Every template, in display order.
/// </summary>
/// <param name="Rows">The templates.</param>
public sealed record SearchServiceTemplatesResponse(
    IReadOnlyList<SearchServiceTemplatesResponse.Row> Rows)
{
    /// <summary>One pre-written request.</summary>
    /// <param name="Id">The template.</param>
    /// <param name="TemplateName">Unique, enforced by UX_ServiceTemplate_Name.</param>
    /// <param name="RequestKind">Support, AssetFault or NewService. Not editable after creation.</param>
    /// <param name="RequestCategoryId">The category it pre-fills.</param>
    /// <param name="RequestSubCategoryId">The sub-category it pre-fills.</param>
    /// <param name="DefaultPriority">Low, Medium, High or Critical.</param>
    /// <param name="DefaultSupportTeamId">The queue it goes to.</param>
    /// <param name="SubjectTemplate">The subject the raise screen starts from.</param>
    /// <param name="DescriptionTemplate">The body it starts from.</param>
    /// <param name="RequiresAsset">Whether the raise screen must ask which asset.</param>
    /// <param name="DisplayOrder">The order the picker shows them in.</param>
    /// <param name="IsActive">Retired templates stay: tickets raised from them keep the link.</param>
    public sealed record Row(
        int Id,
        string TemplateName,
        string RequestKind,
        int? RequestCategoryId,
        int? RequestSubCategoryId,
        string DefaultPriority,
        int? DefaultSupportTeamId,
        string SubjectTemplate,
        string? DescriptionTemplate,
        bool RequiresAsset,
        int DisplayOrder,
        bool IsActive);
}
