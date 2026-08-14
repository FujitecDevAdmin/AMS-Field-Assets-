namespace AMS.Modules.ServiceLevel.Domain;

/// <summary>
/// Mirrors <c>[ServiceLevel].[SlaEscalation]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class SlaEscalation
{
    public int Id { get; set; }

    public int SlaPolicyId { get; set; }

    public required string EscalationType { get; set; }

    public int Level { get; set; }

    public int ThresholdPercent { get; set; }

    public required string RecipientType { get; set; }

    public string? RecipientAddress { get; set; }

    public required string Channel { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
