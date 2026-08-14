namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[CustomFieldOption]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class CustomFieldOption
{
    public int Id { get; set; }

    public int CustomFieldDefinitionId { get; set; }

    public required string OptionValue { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
