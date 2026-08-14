using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceLevel.Features.CreateHoliday;

/// <summary>
/// Add a holiday. Catalogue: Holiday Calendar.
/// </summary>
public sealed record CreateHolidayCommand(
    string HolidayName,
    DateOnly HolidayDate,
    string HolidayType,
    bool AppliesToAllLocations,
    bool IsRecurringAnnually,
    string? Remarks,
    IReadOnlyList<int> LocationIds) : ICommand<CreateHolidayResponse>;
