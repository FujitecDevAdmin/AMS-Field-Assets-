namespace AMS.Modules.Contracts.Domain;

/// <summary>
/// Mirrors <c>[Contracts].[ContractReminderLog]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class ContractReminderLog
{
    public long Id { get; set; }

    public int ContractId { get; set; }

    public int DaysBeforeExpiry { get; set; }

    public DateOnly ExpiryDateSnapshot { get; set; }

    public DateOnly SentOnDate { get; set; }

    public string? SentTo { get; set; }

    public long? EmailOutboxId { get; set; }

    /// <summary>Defaults to <c>N'Queued'</c>, as <c>DF_ContractReminderLog_Outcome</c> does.</summary>
    public string Outcome { get; set; } = "Queued";
}
