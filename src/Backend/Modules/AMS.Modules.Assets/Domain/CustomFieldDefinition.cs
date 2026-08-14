namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[CustomFieldDefinition]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class CustomFieldDefinition
{
    public int Id { get; set; }

    public int AssetTypeId { get; set; }

    public required string FieldName { get; set; }

    public required string DisplayLabel { get; set; }

    public required string FieldType { get; set; }

    public bool IsRequired { get; set; }

    public decimal? MinValue { get; set; }

    public decimal? MaxValue { get; set; }

    public string? ValidationRegex { get; set; }

    public string? DefaultValue { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
