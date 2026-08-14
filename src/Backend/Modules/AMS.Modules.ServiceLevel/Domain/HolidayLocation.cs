namespace AMS.Modules.ServiceLevel.Domain;

/// <summary>
/// Mirrors <c>[ServiceLevel].[HolidayLocation]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class HolidayLocation
{
    public int HolidayCalendarId { get; set; }

    public int LocationId { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }
}
