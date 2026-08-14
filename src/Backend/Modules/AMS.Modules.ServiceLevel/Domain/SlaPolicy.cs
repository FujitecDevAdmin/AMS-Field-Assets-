namespace AMS.Modules.ServiceLevel.Domain;

/// <summary>
/// Mirrors <c>[ServiceLevel].[SlaPolicy]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
/// <remarks>
/// System-versioned. Prior versions live in <c>[ServiceLevel].[SlaPolicyHistory]</c>,
/// readable with <c>TemporalAsOf</c>. The concurrency token is
/// <c>ConcurrencyStamp</c>, NOT the period columns (R2-22).
/// </remarks>
public sealed class SlaPolicy
{
    public int Id { get; set; }

    public required string PolicyName { get; set; }

    public string? Description { get; set; }

    public required string Priority { get; set; }

    public int ResponseTargetMinutes { get; set; }

    public int ResolutionTargetMinutes { get; set; }

    /// <summary>Defaults to <c>1</c>, as <c>DF_SlaPolicy_RespectOperationalHours</c> does.</summary>
    public bool RespectOperationalHours { get; set; } = true;

    /// <summary>Defaults to <c>1</c>, as <c>DF_SlaPolicy_RespectHolidays</c> does.</summary>
    public bool RespectHolidays { get; set; } = true;

    /// <summary>Defaults to <c>1</c>, as <c>DF_SlaPolicy_RespectWeekends</c> does.</summary>
    public bool RespectWeekends { get; set; } = true;

    /// <summary>Defaults to <c>30</c>, as <c>DF_SlaPolicy_NearDueWarningMinutes</c> does.</summary>
    public int NearDueWarningMinutes { get; set; } = 30;

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    public Guid ConcurrencyStamp { get; set; }
}
