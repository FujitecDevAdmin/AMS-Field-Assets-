namespace AMS.Modules.ServiceLevel.Domain;

/// <summary>
/// Mirrors <c>[ServiceLevel].[LocationSaturdayRule]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class LocationSaturdayRule
{
    public int Id { get; set; }

    public int LocationOperationalHourId { get; set; }

    public byte Occurrence { get; set; }

    public bool IsWorking { get; set; }
}
