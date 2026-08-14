using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceLevel.Features.SetHolidayLocations;

/// <summary>
/// Say which branches observe a regional holiday. Catalogue: Holiday Calendar.
/// </summary>
public sealed record SetHolidayLocationsCommand(
    int Id,
    IReadOnlyList<int> LocationIds) : ICommand<SetHolidayLocationsResponse>;
