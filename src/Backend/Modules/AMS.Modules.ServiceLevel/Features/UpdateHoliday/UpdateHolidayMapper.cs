using AMS.Modules.ServiceLevel.Domain;

namespace AMS.Modules.ServiceLevel.Features.UpdateHoliday;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateHolidayMapper
{
    public static UpdateHolidayCommand ToCommand(UpdateHolidayRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateHolidayCommand(
            id,
            request.HolidayName.Trim(),
            request.HolidayDate,
            string.IsNullOrWhiteSpace(request.HolidayType) ? HolidayType.Government : request.HolidayType.Trim(),
            request.AppliesToAllLocations ?? false,
            request.IsRecurringAnnually ?? false,
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
            request.IsActive ?? true);
    }
}
