namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[AssetCustomValue]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetCustomValue
{
    public int Id { get; set; }

    public int AssetId { get; set; }

    public int CustomFieldDefinitionId { get; set; }

    public string? Value { get; set; }

    public decimal? ValueNumber { get; set; }

    public DateOnly? ValueDate { get; set; }

    public int? OptionId { get; set; }

    public DateTime UpdatedOnUtc { get; set; }

    public string? UpdatedBy { get; set; }
}
