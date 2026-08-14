using AMS.Modules.ServiceLevel.Domain;

namespace AMS.Modules.ServiceLevel.Features.CreateHoliday;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateHolidayMapper
{
    public static CreateHolidayCommand ToCommand(CreateHolidayRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateHolidayCommand(
            request.HolidayName.Trim(),
            request.HolidayDate,
            string.IsNullOrWhiteSpace(request.HolidayType) ? HolidayType.Government : request.HolidayType.Trim(),
            request.AppliesToAllLocations ?? false,
            request.IsRecurringAnnually ?? false,
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
            request.LocationIds);
    }
}
