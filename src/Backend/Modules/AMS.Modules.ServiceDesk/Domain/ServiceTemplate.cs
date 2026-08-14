namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[ServiceTemplate]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class ServiceTemplate
{
    public int Id { get; set; }

    public required string TemplateName { get; set; }

    public required string RequestKind { get; set; }

    public int? RequestCategoryId { get; set; }

    public int? RequestSubCategoryId { get; set; }

    public required string DefaultPriority { get; set; }

    public int? DefaultSupportTeamId { get; set; }

    public required string SubjectTemplate { get; set; }

    public string? DescriptionTemplate { get; set; }

    public bool RequiresAsset { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
