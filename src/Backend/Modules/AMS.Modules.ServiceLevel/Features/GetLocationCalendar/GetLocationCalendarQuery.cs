using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceLevel.Features.GetLocationCalendar;

/// <summary>
/// One branch's working week. Catalogue: Operational Hours Setup.
/// </summary>
public sealed record GetLocationCalendarQuery(
    int LocationId) : IQuery<GetLocationCalendarResponse>;
