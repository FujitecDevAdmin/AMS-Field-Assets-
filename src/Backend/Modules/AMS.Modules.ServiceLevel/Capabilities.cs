namespace AMS.Modules.ServiceLevel;

/// <summary>
/// The capability names this module's endpoints declare, spelled exactly as
/// Section 17.6 of the design script seeds them.
/// </summary>
/// <remarks>
/// All three were already in the seed before a line of this module was
/// written — the first module for which that is true. The reason is worth
/// keeping: this module's screens were designed with the schema, not
/// discovered while writing slices.
///
/// There is no separate view capability. The only screens that read a policy
/// or a calendar are the screens that edit them; the calendar arithmetic
/// itself is called by other modules through a contract, not by a person
/// through an endpoint, so there is nobody to grant a read-only right to.
/// </remarks>
public static class Capabilities
{
    public static class ServiceLevel
    {
        /// <summary>Create and edit SLA policies and their escalation levels.</summary>
        public const string SlaManage = "sla.manage";

        /// <summary>Configure a branch's operational hours, days and Saturday rules.</summary>
        public const string CalendarManage = "calendar.manage";

        /// <summary>Maintain the holiday calendar and which branches observe each holiday.</summary>
        public const string HolidayManage = "holiday.manage";
    }
}
