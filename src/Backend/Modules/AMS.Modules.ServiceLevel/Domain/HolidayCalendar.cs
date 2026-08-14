namespace AMS.Modules.ServiceLevel.Domain;

/// <summary>
/// Mirrors <c>[ServiceLevel].[HolidayCalendar]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class HolidayCalendar
{
    public int Id { get; set; }

    public required string HolidayName { get; set; }

    public DateOnly HolidayDate { get; set; }

    public int HolidayYear { get; set; }

    public required string HolidayType { get; set; }

    public bool AppliesToAllLocations { get; set; }

    public bool IsRecurringAnnually { get; set; }

    public byte? RecurrenceMonth { get; set; }

    public byte? RecurrenceDay { get; set; }

    public string? Remarks { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
