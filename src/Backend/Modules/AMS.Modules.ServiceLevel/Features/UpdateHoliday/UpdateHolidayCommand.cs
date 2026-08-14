using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceLevel.Features.UpdateHoliday;

/// <summary>
/// Edit a holiday or retire it. Catalogue: Holiday Calendar.
/// </summary>
public sealed record UpdateHolidayCommand(
    int Id,
    string HolidayName,
    DateOnly HolidayDate,
    string HolidayType,
    bool AppliesToAllLocations,
    bool IsRecurringAnnually,
    string? Remarks,
    bool IsActive) : ICommand<UpdateHolidayResponse>;
