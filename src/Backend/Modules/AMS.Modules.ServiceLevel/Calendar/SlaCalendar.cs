using AMS.Modules.ServiceLevel.Domain;

namespace AMS.Modules.ServiceLevel.Calendar;

/// <summary>The branch's calendar as one policy sees it.</summary>
/// <remarks>
/// <para>
/// The three <c>Respect*</c> flags are subtractive: each one a policy turns off
/// removes a reason the branch would otherwise be shut. A policy that ignores
/// holidays measures Republic Day as an ordinary working day; one that ignores
/// weekends measures Sunday as one; one that ignores operational hours measures
/// the whole day.
/// </para>
/// <para>
/// Done by editing the snapshot rather than threading three flags through the
/// arithmetic, so <see cref="OperationalCalendar"/> stays a calculator that
/// knows nothing about policies.
/// </para>
/// <para>
/// It lives here, on its own, because two things need it: the calculator that
/// sets due dates and the monitor that decides when a missed one escalates.
/// Two copies of "ignoring weekends also means ignoring the Saturday rules"
/// would be two chances for a due date and its escalation to disagree.
/// </para>
/// </remarks>
public static class SlaCalendar
{
    /// <summary>Whether this policy looks at the calendar at all.</summary>
    public static bool RespectsCalendar(SlaPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return policy.RespectOperationalHours
            || policy.RespectHolidays
            || policy.RespectWeekends;
    }

    /// <summary>The calendar with everything this policy ignores removed.</summary>
    public static CalendarSnapshot AsSeenBy(CalendarSnapshot calendar, SlaPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        ArgumentNullException.ThrowIfNull(policy);

        if (!policy.RespectHolidays)
        {
            calendar = calendar with
            {
                FixedHolidays = new HashSet<DateOnly>(),
                RecurringHolidays = new HashSet<(int, int)>(),
            };
        }

        if (!policy.RespectWeekends)
        {
            calendar = calendar with
            {
                Days = [.. calendar.Days.Select(d => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                    ? d with { IsWorkingDay = true }
                    : d)],
                // A Saturday rule is a weekend rule. Leaving it in place would
                // let "ignore weekends" still close the second Saturday.
                WorkingSaturdays = new HashSet<int>(),
            };
        }

        if (!policy.RespectOperationalHours)
        {
            calendar = calendar with { IsRoundTheClock = true };
        }

        return calendar;
    }
}
