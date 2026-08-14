using AMS.Modules.Notifications.Features.CreateEmailSetting;
using AMS.Modules.Notifications.Features.MarkNotificationsRead;
using AMS.Modules.Notifications.Features.RequeueEmail;
using AMS.Modules.Notifications.Features.SearchEmailOutbox;
using AMS.Modules.Notifications.Features.SearchEmailSettings;
using AMS.Modules.Notifications.Features.SearchMyNotifications;
using AMS.Modules.Notifications.Features.UpdateEmailSetting;
using AMS.Modules.Notifications.Persistence;
using AMS.Modules.Notifications.PublicApi.Notifications;
using AMS.Modules.Notifications.Sending;
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

namespace AMS.Modules.Notifications;

/// <summary>
/// The module's single registration point. <c>Program.cs</c> is a list of
/// these calls and nothing else (docs/02 §9).
/// </summary>
/// <remarks>
/// Three tables and one job: this is the only way anything in the system tells
/// somebody something. Every e-mail goes through the outbox — ticket replies,
/// approval requests, SLA escalations, contract reminders — because sending
/// inline from a request thread loses the message when SMTP is down, and
/// nobody finds out.
/// </remarks>
public static class NotificationsModuleExtensions
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<NotificationsDbContext>(NotificationsDbContext.SchemaName);

        // Rule 3: what every other module may ask this one. Write-only — a
        // module may ask for somebody to be told; none of them may read another
        // user's notifications or another module's queue.
        services.AddScoped<INotifier, Notifier>();

        services.AddScoped<SmtpPasswordProtector>();

        AddHandlers(services);
        AddDispatcher(services, configuration);

        services.AddValidatorsFromAssemblyContaining<CreateEmailSettingValidator>(
            ServiceLifetime.Scoped);

        // Every unique index in this schema, with the 409 it produces.
        // SqlErrorRegistrationTests reads the live schema and fails if one is
        // missing from this list (docs/03 §7).
        services.AddSingleton(new SqlErrorTranslator()
            .Register("UX_EmailSetting_Name", "EmailSetting.NameTaken",
                "A profile with that name already exists.")
            // A filtered unique index over IsDefault = 1. Making a second
            // profile the default collides here rather than silently demoting
            // the one somebody else chose.
            .Register("UX_EmailSetting_OneDefault", "EmailSetting.DefaultExists",
                "Another profile is already the default. Clear that one first."));

        return services;
    }

    /// <summary>Contributes this module's routes (01 §5).</summary>
    public static IEndpointRouteBuilder MapNotificationsModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/notifications")
            .RequireAuthorization()
            .AddEndpointFilter<ValidationEndpointFilter>();

        // The bell.
        SearchMyNotificationsEndpoint.Map(group);
        MarkNotificationsReadEndpoint.Map(group);

        // SMTP profiles.
        SearchEmailSettingsEndpoint.Map(group);
        CreateEmailSettingEndpoint.Map(group);
        UpdateEmailSettingEndpoint.Map(group);

        // The queue.
        SearchEmailOutboxEndpoint.Map(group);
        RequeueEmailEndpoint.Map(group);

        return endpoints;
    }

    /// <summary>
    /// The thing that drains the queue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Configurable through <c>Notifications:Dispatcher</c>, because the right
    /// poll interval on a laptop and on a production host are not the same
    /// number, and neither is worth a release to change.
    /// </para>
    /// <para>
    /// The hosted service can be switched off with
    /// <c>Notifications:Dispatcher:Enabled = false</c>. Integration tests and
    /// the design-time migration host both want the module without a background
    /// thread sending real e-mail, and so does anybody debugging a queue.
    /// </para>
    /// </remarks>
    private static void AddDispatcher(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Notifications:Dispatcher");

        var options = new DispatcherOptions(
            PollSeconds: section.GetValue("PollSeconds", 15),
            BatchSize: section.GetValue("BatchSize", 20),
            MaxAttempts: section.GetValue("MaxAttempts", 5));

        services.AddSingleton(options);
        services.AddSingleton<IEmailTransport, SmtpEmailTransport>();
        services.AddScoped<EmailDispatcher>();

        if (section.GetValue("Enabled", true))
        {
            services.AddHostedService<EmailDispatcherService>();
        }
    }

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<SearchMyNotificationsQuery, SearchMyNotificationsResponse>, SearchMyNotificationsHandler>();
        services.AddScoped<IRequestHandler<MarkNotificationsReadCommand, MarkNotificationsReadResponse>, MarkNotificationsReadHandler>();

        services.AddScoped<IRequestHandler<SearchEmailSettingsQuery, SearchEmailSettingsResponse>, SearchEmailSettingsHandler>();
        services.AddScoped<IRequestHandler<CreateEmailSettingCommand, CreateEmailSettingResponse>, CreateEmailSettingHandler>();
        services.AddScoped<IRequestHandler<UpdateEmailSettingCommand, UpdateEmailSettingResponse>, UpdateEmailSettingHandler>();

        services.AddScoped<IRequestHandler<SearchEmailOutboxQuery, SearchEmailOutboxResponse>, SearchEmailOutboxHandler>();
        services.AddScoped<IRequestHandler<RequeueEmailCommand, RequeueEmailResponse>, RequeueEmailHandler>();
    }
}
