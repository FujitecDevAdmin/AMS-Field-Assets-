"""
Slice specifications for the ServiceLevel module, pass one: the operational
calendar.

Catalogue screens: Operational Hours Setup and the Holiday Calendar. This is
the half of the module that answers "is this minute operational for this
branch"; pass two answers "is this ticket late", and cannot be written first
because every due date it produces is measured in these minutes.

Two things shape the slices:

  * A calendar is ONE thing. The standard window, the seven weekdays and the
    five Saturday occurrence rules arrive together, because a half-saved
    calendar is one the SLA service would read and believe.

  * Every wall-clock time here is LOCAL to the branch. A branch opens at 09:00
    where it stands, and the conversion happens once, at the edge.

    python build/servicelevel_slices_calendar.py
"""
from slices import main

NS = "AMS.Modules.ServiceLevel"
PROJECT = "AMS.Modules.ServiceLevel"

CALENDAR = "Capabilities.ServiceLevel.CalendarManage"
HOLIDAY = "Capabilities.ServiceLevel.HolidayManage"

SPECS = [
    # ------------------------------------------------------------- calendar
    {
        "name": "GetLocationCalendar", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "One branch's working week. Catalogue: Operational Hours Setup.",
        "capability": CALENDAR,
        "verb": "Get", "route": "/locations/{locationId:int}/calendar",
        "command": [("int", "LocationId")],
        "request": [],
        "response": [("int", "LocationId"), ("bool", "IsConfigured"), ("bool", "IsRoundTheClock"),
                     ("TimeOnly", "StandardStartTime"), ("TimeOnly", "StandardEndTime"),
                     ("TimeOnly?", "BreakStartTime"), ("TimeOnly?", "BreakEndTime"),
                     ("int", "DeferFinalMinutes"), ("bool", "DeferNewTicketsOnFriday"),
                     ("IReadOnlyList<GetLocationCalendarResponse.Day>", "Days"),
                     ("IReadOnlyList<GetLocationCalendarResponse.Saturday>", "Saturdays")],
        "responseSummary": "The branch's week, configured or defaulted.",
        "responseDocs": {
            "LocationId": "The branch.",
            "IsConfigured": "False when nobody has set this branch up and the screen is "
                            "showing the Monday-to-Friday default.",
            "IsRoundTheClock": "A 24-hour branch. The windows below do not apply.",
            "StandardStartTime": "When it opens, local to the branch.",
            "StandardEndTime": "When it closes.",
            "BreakStartTime": "Lunch, if it takes one.",
            "BreakEndTime": "The end of lunch.",
            "DeferFinalMinutes": "A ticket raised this close to closing starts its clock tomorrow.",
            "DeferNewTicketsOnFriday": "A ticket raised on a Friday starts its clock on Monday.",
            "Days": "Seven rows, Sunday first.",
            "Saturdays": "Which Saturdays of the month are worked.",
        },
        "rules": [],
        "mapArgs": ["locationId"],
        "mapCall": "new GetLocationCalendarRequest(), locationId",
        "mapExtra": [("int", "locationId")],
        "bind": "                int locationId,\n",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "SetLocationCalendar", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Set a branch's working week, all of it at once. "
                   "Catalogue: Operational Hours Setup.",
        "capability": CALENDAR,
        "verb": "Put", "route": "/locations/{locationId:int}/calendar",
        "command": [("int", "LocationId"), ("bool", "IsRoundTheClock"),
                    ("TimeOnly", "StandardStartTime"), ("TimeOnly", "StandardEndTime"),
                    ("TimeOnly?", "BreakStartTime"), ("TimeOnly?", "BreakEndTime"),
                    ("int", "DeferFinalMinutes"), ("bool", "DeferNewTicketsOnFriday"),
                    ("IReadOnlyList<SetLocationCalendarCommand.Day>", "Days"),
                    ("IReadOnlyList<int>", "WorkingSaturdays")],
        "request": [("bool?", "IsRoundTheClock"),
                    ("TimeOnly?", "StandardStartTime"), ("TimeOnly?", "StandardEndTime"),
                    ("TimeOnly?", "BreakStartTime"), ("TimeOnly?", "BreakEndTime"),
                    ("int?", "DeferFinalMinutes"), ("bool?", "DeferNewTicketsOnFriday"),
                    ("IReadOnlyList<SetLocationCalendarRequest.Day>", "Days"),
                    ("IReadOnlyList<int>", "WorkingSaturdays")],
        "response": [("int", "LocationId"), ("int", "WorkingDayCount"),
                     ("int", "WorkingSaturdayCount")],
        "responseSummary": "The week as it now stands.",
        "responseDocs": {
            "LocationId": "The branch.",
            "WorkingDayCount": "How many weekdays are worked.",
            "WorkingSaturdayCount": "How many Saturdays of the month, when Saturday is worked at all.",
        },
        "rules": [
            "RuleFor(x => x.DeferFinalMinutes).InclusiveBetween(0, 480)"
            ".When(x => x.DeferFinalMinutes.HasValue);",
            "RuleForEach(x => x.WorkingSaturdays).InclusiveBetween(1, 5);",
        ],
        "mapArgs": ["locationId", "request.IsRoundTheClock ?? false",
                    "request.StandardStartTime ?? new TimeOnly(9, 0)",
                    "request.StandardEndTime ?? new TimeOnly(18, 0)",
                    "request.BreakStartTime", "request.BreakEndTime",
                    "request.DeferFinalMinutes ?? 30", "request.DeferNewTicketsOnFriday ?? false",
                    "[.. request.Days.Select(d => new SetLocationCalendarCommand.Day(\n"
                    "                d.DayOfWeek,\n"
                    "                d.IsWorkingDay,\n"
                    "                string.IsNullOrWhiteSpace(d.DayType) ? CalendarDayType.Standard : d.DayType.Trim(),\n"
                    "                d.StartTime,\n"
                    "                d.EndTime,\n"
                    "                d.BreakStartTime,\n"
                    "                d.BreakEndTime))]",
                    "request.WorkingSaturdays"],
        "mapCall": "request, locationId",
        "mapExtra": [("int", "locationId")],
        "bind": "                int locationId,\n                SetLocationCalendarRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },

    # ------------------------------------------------------------- holidays
    {
        "name": "SearchHolidays", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The holiday calendar. Catalogue: Holiday Calendar.",
        "capability": HOLIDAY,
        "verb": "Get", "route": "/holidays",
        "command": [("int?", "Year"), ("string?", "HolidayType"), ("int?", "LocationId"),
                    ("bool", "ActiveOnly")],
        "request": [("int?", "Year"), ("string?", "HolidayType"), ("int?", "LocationId"),
                    ("bool?", "ActiveOnly")],
        "response": [("IReadOnlyList<SearchHolidaysResponse.Row>", "Rows")],
        "responseSummary": "Holidays, earliest first.",
        "responseDocs": {"Rows": "The list, each with the branches that observe it."},
        "rules": [
            "RuleFor(x => x.Year).InclusiveBetween(2000, 2100).When(x => x.Year.HasValue);",
            "RuleFor(x => x.HolidayType).MaximumLength(20);",
        ],
        "mapArgs": ["request.Year",
                    "string.IsNullOrWhiteSpace(request.HolidayType) ? null : request.HolidayType.Trim()",
                    "request.LocationId", "request.ActiveOnly ?? true"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchHolidaysRequest request,\n",
    },
    {
        "name": "CreateHoliday", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Add a holiday. Catalogue: Holiday Calendar.",
        "capability": HOLIDAY,
        "verb": "Post", "route": "/holidays",
        "command": [("string", "HolidayName"), ("DateOnly", "HolidayDate"), ("string", "HolidayType"),
                    ("bool", "AppliesToAllLocations"), ("bool", "IsRecurringAnnually"),
                    ("string?", "Remarks"), ("IReadOnlyList<int>", "LocationIds")],
        "request": [("string", "HolidayName"), ("DateOnly", "HolidayDate"), ("string?", "HolidayType"),
                    ("bool?", "AppliesToAllLocations"), ("bool?", "IsRecurringAnnually"),
                    ("string?", "Remarks"), ("IReadOnlyList<int>", "LocationIds")],
        "response": [("int", "Id"), ("string", "HolidayName"), ("DateOnly", "HolidayDate"),
                     ("int", "LocationCount")],
        "responseSummary": "The holiday, as entered.",
        "responseDocs": {
            "Id": "The holiday.",
            "HolidayName": "What it is called.",
            "HolidayDate": "The date it falls on this year.",
            "LocationCount": "How many branches observe it. Zero when it applies to all of them.",
        },
        "rules": [
            "RuleFor(x => x.HolidayName).NotEmpty().MaximumLength(150);",
            "RuleFor(x => x.HolidayType).MaximumLength(20);",
            "RuleFor(x => x.Remarks).MaximumLength(300);",
        ],
        "mapArgs": ["request.HolidayName.Trim()", "request.HolidayDate",
                    "string.IsNullOrWhiteSpace(request.HolidayType) ? HolidayType.Government : request.HolidayType.Trim()",
                    "request.AppliesToAllLocations ?? false",
                    "request.IsRecurringAnnually ?? false",
                    "string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim()",
                    "request.LocationIds"],
        "mapCall": "request",
        "bind": "                CreateHolidayRequest request,\n",
        "successStatus": "Status201Created",
        "otherStatuses": ["Status409Conflict"],
    },
    {
        "name": "UpdateHoliday", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Edit a holiday or retire it. Catalogue: Holiday Calendar.",
        "capability": HOLIDAY,
        "verb": "Put", "route": "/holidays/{id:int}",
        "command": [("int", "Id"), ("string", "HolidayName"), ("DateOnly", "HolidayDate"),
                    ("string", "HolidayType"), ("bool", "AppliesToAllLocations"),
                    ("bool", "IsRecurringAnnually"), ("string?", "Remarks"), ("bool", "IsActive")],
        "request": [("string", "HolidayName"), ("DateOnly", "HolidayDate"), ("string?", "HolidayType"),
                    ("bool?", "AppliesToAllLocations"), ("bool?", "IsRecurringAnnually"),
                    ("string?", "Remarks"), ("bool?", "IsActive")],
        "response": [("int", "Id"), ("string", "HolidayName"), ("bool", "IsActive")],
        "responseSummary": "The holiday as it now stands.",
        "responseDocs": {
            "Id": "The holiday.",
            "HolidayName": "What it is called.",
            "IsActive": "Whether the calendar still observes it.",
        },
        "rules": [
            "RuleFor(x => x.HolidayName).NotEmpty().MaximumLength(150);",
            "RuleFor(x => x.HolidayType).MaximumLength(20);",
            "RuleFor(x => x.Remarks).MaximumLength(300);",
        ],
        "mapArgs": ["id", "request.HolidayName.Trim()", "request.HolidayDate",
                    "string.IsNullOrWhiteSpace(request.HolidayType) ? HolidayType.Government : request.HolidayType.Trim()",
                    "request.AppliesToAllLocations ?? false",
                    "request.IsRecurringAnnually ?? false",
                    "string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim()",
                    "request.IsActive ?? true"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                UpdateHolidayRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "SetHolidayLocations", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Say which branches observe a regional holiday. Catalogue: Holiday Calendar.",
        "capability": HOLIDAY,
        "verb": "Put", "route": "/holidays/{id:int}/locations",
        "command": [("int", "Id"), ("IReadOnlyList<int>", "LocationIds")],
        "request": [("IReadOnlyList<int>", "LocationIds")],
        "response": [("int", "Id"), ("int", "LocationCount")],
        "responseSummary": "How many branches observe it now.",
        "responseDocs": {
            "Id": "The holiday.",
            "LocationCount": "The branches attached to it.",
        },
        "rules": [],
        "mapArgs": ["id", "request.LocationIds"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                SetHolidayLocationsRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
]

if __name__ == "__main__":
    main(SPECS)
