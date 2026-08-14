namespace AMS.Modules.Contracts.Domain;

/// <summary>
/// Mirrors <c>[Contracts].[ContractReminderSetting]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class ContractReminderSetting
{
    public int Id { get; set; }

    public int? ContractId { get; set; }

    public int DaysBeforeExpiry { get; set; }

    public string? Recipients { get; set; }

    /// <summary>Defaults to <c>N'Email'</c>, as <c>DF_ContractReminderSetting_Channel</c> does.</summary>
    public string Channel { get; set; } = "Email";

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
