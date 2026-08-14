using AMS.Modules.ServiceLevel.Escalation;
using AMS.Modules.ServiceLevel.PublicApi;
using AMS.Modules.ServiceLevel.PublicApi.ServiceLevel;
using AMS.Modules.ServiceLevel.Features.CreateSlaPolicy;
using AMS.Modules.ServiceLevel.Features.SearchEscalationLog;
using AMS.Modules.ServiceLevel.Features.SearchSlaPolicies;
using AMS.Modules.ServiceLevel.Features.SetSlaEscalations;
using AMS.Modules.ServiceLevel.Features.UpdateSlaPolicy;
using AMS.Modules.ServiceLevel.Calendar;
using AMS.Modules.ServiceLevel.Features.CreateHoliday;
using AMS.Modules.ServiceLevel.Features.GetLocationCalendar;
using AMS.Modules.ServiceLevel.Features.SearchHolidays;
using AMS.Modules.ServiceLevel.Features.SetHolidayLocations;
using AMS.Modules.ServiceLevel.Features.SetLocationCalendar;
using AMS.Modules.ServiceLevel.Features.UpdateHoliday;
using AMS.Modules.ServiceLevel.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Persistence.Transactions;
using AMS.SharedKernel.Web.Http;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AMS.Modules.ServiceLevel;

/// <summary>
/// The module's single registration point. <c>Program.cs</c> is a list of
/// these calls and nothing else (docs/02 §9).
/// </summary>
/// <remarks>
/// Two halves, and the order mattered. The operational calendar answers "is
/// this minute operational for this branch"; the policies answer "is this
/// ticket late", and every due date they produce is measured in those minutes.
/// </remarks>
public static class ServiceLevelModuleExtensions
{
    public static IServiceCollection AddServiceLevelModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<ServiceLevelDbContext>(ServiceLevelDbContext.SchemaName);

        // Scoped, so its cache lasts one request. A calendar edited on the
        // setup screen takes effect on the next request rather than whenever a
        // process happens to restart.
        services.AddScoped<CalendarLoader>();

        // Rule 3: what ServiceDesk may ask this module, and the only way it may
        // ask it. Read-only - a target quietly changed by the module it judges
        // is not a target.
        services.AddScoped<ISlaCalculator, SlaCalculator>();

        // The last thing the schema was written for: SlaEscalation holds the
        // ladder, SlaEscalationLog holds the evidence, and this fires it.
        services.AddScoped<SlaEscalationMonitor>();

        AddHandlers(services);

        services.AddValidatorsFromAssemblyContaining<CreateHolidayValidator>(
            ServiceLifetime.Scoped);

        // Every unique index in this schema, with the 409 it produces.
        // SqlErrorRegistrationTests reads the live schema and fails if one is
        // missing from this list (docs/03 §7).
        services.AddSingleton(new SqlErrorTranslator()
            .Register("UX_SlaPolicy_Name", "SlaPolicy.NameTaken",
                "A policy with that name already exists.")
            // One live policy per priority. Two active "High" policies means a
            // ticket gets whichever the query happened to order first.
            .Register("UX_SlaPolicy_ActivePriority", "SlaPolicy.PriorityTaken",
                "Another active policy already covers that priority. Retire it first.")
            .Register("UX_SlaEscalation_PolicyTypeLevel", "SlaEscalation.LevelTaken",
                "That policy already has an escalation at this level.")
            // R2-3: the index excludes Outcome = 'Failed', so a failed attempt
            // can be retried while a Sent or Skipped row still blocks a repeat.
            .Register("UX_SlaEscalationLog_OncePerLevel", "SlaEscalation.AlreadyFired",
                "That escalation has already fired for this ticket.")
            .Register("UX_LocationOperationalHour_Location", "LocationCalendar.Exists",
                "That branch already has a calendar.")
            .Register("UX_LocationOperationalDay_Day", "LocationCalendar.DayTaken",
                "That weekday is already set for this branch.")
            .Register("UX_LocationSaturdayRule_Occurrence", "LocationCalendar.SaturdayTaken",
                "That Saturday occurrence is already set for this branch."));

        return services;
    }

    /// <summary>Contributes this module's routes (01 §5).</summary>
    public static IEndpointRouteBuilder MapServiceLevelModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/service-level")
            .RequireAuthorization()
            .AddEndpointFilter<ValidationEndpointFilter>();

        // The working week.
        GetLocationCalendarEndpoint.Map(group);
        SetLocationCalendarEndpoint.Map(group);

        // The holiday calendar.
        SearchHolidaysEndpoint.Map(group);
        CreateHolidayEndpoint.Map(group);
        UpdateHolidayEndpoint.Map(group);
        SetHolidayLocationsEndpoint.Map(group);

        // Policies and escalation.
        SearchSlaPoliciesEndpoint.Map(group);
        CreateSlaPolicyEndpoint.Map(group);
        UpdateSlaPolicyEndpoint.Map(group);
        SetSlaEscalationsEndpoint.Map(group);
        SearchEscalationLogEndpoint.Map(group);

        return endpoints;
    }

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<GetLocationCalendarQuery, GetLocationCalendarResponse>, GetLocationCalendarHandler>();
        services.AddScoped<IRequestHandler<SetLocationCalendarCommand, SetLocationCalendarResponse>, SetLocationCalendarHandler>();

        services.AddScoped<IRequestHandler<SearchHolidaysQuery, SearchHolidaysResponse>, SearchHolidaysHandler>();
        services.AddScoped<IRequestHandler<CreateHolidayCommand, CreateHolidayResponse>, CreateHolidayHandler>();
        services.AddScoped<IRequestHandler<UpdateHolidayCommand, UpdateHolidayResponse>, UpdateHolidayHandler>();
        services.AddScoped<IRequestHandler<SetHolidayLocationsCommand, SetHolidayLocationsResponse>, SetHolidayLocationsHandler>();

        services.AddScoped<IRequestHandler<SearchSlaPoliciesQuery, SearchSlaPoliciesResponse>, SearchSlaPoliciesHandler>();
        services.AddScoped<IRequestHandler<CreateSlaPolicyCommand, CreateSlaPolicyResponse>, CreateSlaPolicyHandler>();
        services.AddScoped<IRequestHandler<UpdateSlaPolicyCommand, UpdateSlaPolicyResponse>, UpdateSlaPolicyHandler>();
        services.AddScoped<IRequestHandler<SetSlaEscalationsCommand, SetSlaEscalationsResponse>, SetSlaEscalationsHandler>();
        services.AddScoped<IRequestHandler<SearchEscalationLogQuery, SearchEscalationLogResponse>, SearchEscalationLogHandler>();
    }
}
