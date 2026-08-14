using AMS.Modules.ServiceDesk.PublicApi;
using AMS.Modules.ServiceDesk.PublicApi.ServiceDesk;
using AMS.Modules.ServiceDesk.Approvals;
using AMS.Modules.ServiceDesk.Features.CancelApproval;
using AMS.Modules.ServiceDesk.Features.CreateApprovalWorkflow;
using AMS.Modules.ServiceDesk.Features.DecideApproval;
using AMS.Modules.ServiceDesk.Features.GetRequestApproval;
using AMS.Modules.ServiceDesk.Features.PublishApprovalWorkflow;
using AMS.Modules.ServiceDesk.Features.SearchApprovalWorkflows;
using AMS.Modules.ServiceDesk.Features.SearchMyApprovals;
using AMS.Modules.ServiceDesk.Features.SubmitForApproval;
using AMS.Modules.ServiceDesk.Features.AddRequestAttachment;
using AMS.Modules.ServiceDesk.Features.AddRequestNote;
using AMS.Modules.ServiceDesk.Features.AssignServiceRequest;
using AMS.Modules.ServiceDesk.Features.ChangeRequestStatus;
using AMS.Modules.ServiceDesk.Features.GetServiceRequest;
using AMS.Modules.ServiceDesk.Features.RaiseServiceRequest;
using AMS.Modules.ServiceDesk.Features.SearchMyRequests;
using AMS.Modules.ServiceDesk.Features.SearchRequestQueue;
using AMS.Modules.ServiceDesk.Features.SendRequestEmail;
using AMS.Modules.ServiceDesk.Features.CreateRequestCategory;
using AMS.Modules.ServiceDesk.Features.CreateRequestSubCategory;
using AMS.Modules.ServiceDesk.Features.CreateServiceTemplate;
using AMS.Modules.ServiceDesk.Features.CreateSupportTeam;
using AMS.Modules.ServiceDesk.Features.SearchRequestCategories;
using AMS.Modules.ServiceDesk.Features.SearchServiceTemplates;
using AMS.Modules.ServiceDesk.Features.SearchSupportTeams;
using AMS.Modules.ServiceDesk.Features.SetSupportTeamMembers;
using AMS.Modules.ServiceDesk.Features.UpdateRequestCategory;
using AMS.Modules.ServiceDesk.Features.UpdateRequestSubCategory;
using AMS.Modules.ServiceDesk.Features.UpdateServiceTemplate;
using AMS.Modules.ServiceDesk.Features.UpdateSupportTeam;
using AMS.Modules.ServiceDesk.Persistence;
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

namespace AMS.Modules.ServiceDesk;

/// <summary>
/// The module's single registration point. <c>Program.cs</c> is a list of
/// these calls and nothing else (docs/02 §9).
/// </summary>
/// <remarks>
/// Being built in three passes, because this module is 20 tables — more than
/// Allocations, Movements and Transfers together. Pass one is the master data
/// a ticket refers to, pass two the tickets themselves, and pass three the
/// approval workflow a new service request runs through.
/// </remarks>
public static class ServiceDeskModuleExtensions
{
    public static IServiceCollection AddServiceDeskModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<ServiceDeskDbContext>(ServiceDeskDbContext.SchemaName);

        // Turns a stage's approver RULES into the people who must decide, using
        // Identity's and Organization's PublicApi contracts (rule 3).
        // Rule 3: what ServiceLevel may ask this module. A generous read and
        // a single narrow write - escalating is telling people, not
        // reassigning work.
        services.AddScoped<ISlaWatchList, SlaWatchList>();

        services.AddScoped<ApproverResolver>();

        // Telling people, and recording that we did. Every approval message
        // goes through the Notifications outbox and leaves a row in
        // ApprovalNotificationLog, which is the answer to "nobody told me".
        services.AddScoped<ApprovalNotifications>();
        services.AddScoped<ApprovalReminderWorker>();

        AddHandlers(services);

        services.AddValidatorsFromAssemblyContaining<CreateRequestCategoryValidator>(
            ServiceLifetime.Scoped);

        // Every unique index in this schema, with the 409 it produces.
        // SqlErrorRegistrationTests reads the live schema and fails if one is
        // missing from this list (docs/03 §7).
        services.AddSingleton(new SqlErrorTranslator()
            .Register("UX_RequestStatus_Name", "RequestStatus.NameTaken",
                "A ticket status with that name already exists.")
            .Register("UX_RequestCategory_Name", "RequestCategory.NameTaken",
                "A category with that name already exists.")
            .Register("UX_RequestSubCategory_Name", "RequestSubCategory.NameTaken",
                "That category already has a sub-category with this name.")
            .Register("UX_SupportTeam_Name", "SupportTeam.NameTaken",
                "A team with that name already exists.")
            // A filtered unique index over IsDefaultTeam = 1. Making a second
            // team the default collides here rather than silently demoting the
            // one somebody else chose.
            .Register("UX_SupportTeam_OneDefault", "SupportTeam.DefaultExists",
                "Another team is already the default. Clear that one first.")
            .Register("UX_ServiceTemplate_Name", "ServiceTemplate.NameTaken",
                "A template with that name already exists.")
            // Pass two builds the slice behind this one; the index exists now,
            // and an unregistered index is a 500 waiting for the first
            // duplicate.
            .Register("UX_ServiceRequest_Number", "ServiceRequest.NumberTaken",
                "That ticket number is already in use."));

        return services;
    }

    /// <summary>Contributes this module's routes (01 §5).</summary>
    public static IEndpointRouteBuilder MapServiceDeskModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/service-desk")
            .RequireAuthorization()
            .AddEndpointFilter<ValidationEndpointFilter>();

        // Categories and sub-categories.
        SearchRequestCategoriesEndpoint.Map(group);
        CreateRequestCategoryEndpoint.Map(group);
        UpdateRequestCategoryEndpoint.Map(group);
        CreateRequestSubCategoryEndpoint.Map(group);
        UpdateRequestSubCategoryEndpoint.Map(group);

        // Support teams.
        SearchSupportTeamsEndpoint.Map(group);
        CreateSupportTeamEndpoint.Map(group);
        UpdateSupportTeamEndpoint.Map(group);
        SetSupportTeamMembersEndpoint.Map(group);

        // Service templates.
        SearchServiceTemplatesEndpoint.Map(group);
        CreateServiceTemplateEndpoint.Map(group);
        UpdateServiceTemplateEndpoint.Map(group);

        // Tickets: raising, reading, and working one.
        RaiseServiceRequestEndpoint.Map(group);
        SearchMyRequestsEndpoint.Map(group);
        SearchRequestQueueEndpoint.Map(group);
        GetServiceRequestEndpoint.Map(group);
        AssignServiceRequestEndpoint.Map(group);
        ChangeRequestStatusEndpoint.Map(group);
        AddRequestNoteEndpoint.Map(group);
        SendRequestEmailEndpoint.Map(group);
        AddRequestAttachmentEndpoint.Map(group);

        // The approval workflow: its configuration, and the runs it produces.
        SearchApprovalWorkflowsEndpoint.Map(group);
        CreateApprovalWorkflowEndpoint.Map(group);
        PublishApprovalWorkflowEndpoint.Map(group);
        SubmitForApprovalEndpoint.Map(group);
        SearchMyApprovalsEndpoint.Map(group);
        GetRequestApprovalEndpoint.Map(group);
        DecideApprovalEndpoint.Map(group);
        CancelApprovalEndpoint.Map(group);

        return endpoints;
    }

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<SearchRequestCategoriesQuery, SearchRequestCategoriesResponse>, SearchRequestCategoriesHandler>();
        services.AddScoped<IRequestHandler<CreateRequestCategoryCommand, CreateRequestCategoryResponse>, CreateRequestCategoryHandler>();
        services.AddScoped<IRequestHandler<UpdateRequestCategoryCommand, UpdateRequestCategoryResponse>, UpdateRequestCategoryHandler>();
        services.AddScoped<IRequestHandler<CreateRequestSubCategoryCommand, CreateRequestSubCategoryResponse>, CreateRequestSubCategoryHandler>();
        services.AddScoped<IRequestHandler<UpdateRequestSubCategoryCommand, UpdateRequestSubCategoryResponse>, UpdateRequestSubCategoryHandler>();

        services.AddScoped<IRequestHandler<SearchSupportTeamsQuery, SearchSupportTeamsResponse>, SearchSupportTeamsHandler>();
        services.AddScoped<IRequestHandler<CreateSupportTeamCommand, CreateSupportTeamResponse>, CreateSupportTeamHandler>();
        services.AddScoped<IRequestHandler<UpdateSupportTeamCommand, UpdateSupportTeamResponse>, UpdateSupportTeamHandler>();
        services.AddScoped<IRequestHandler<SetSupportTeamMembersCommand, SetSupportTeamMembersResponse>, SetSupportTeamMembersHandler>();

        services.AddScoped<IRequestHandler<SearchServiceTemplatesQuery, SearchServiceTemplatesResponse>, SearchServiceTemplatesHandler>();
        services.AddScoped<IRequestHandler<CreateServiceTemplateCommand, CreateServiceTemplateResponse>, CreateServiceTemplateHandler>();
        services.AddScoped<IRequestHandler<UpdateServiceTemplateCommand, UpdateServiceTemplateResponse>, UpdateServiceTemplateHandler>();

        services.AddScoped<IRequestHandler<RaiseServiceRequestCommand, RaiseServiceRequestResponse>, RaiseServiceRequestHandler>();
        services.AddScoped<IRequestHandler<SearchMyRequestsQuery, SearchMyRequestsResponse>, SearchMyRequestsHandler>();
        services.AddScoped<IRequestHandler<SearchRequestQueueQuery, SearchRequestQueueResponse>, SearchRequestQueueHandler>();
        services.AddScoped<IRequestHandler<GetServiceRequestQuery, GetServiceRequestResponse>, GetServiceRequestHandler>();
        services.AddScoped<IRequestHandler<AssignServiceRequestCommand, AssignServiceRequestResponse>, AssignServiceRequestHandler>();
        services.AddScoped<IRequestHandler<ChangeRequestStatusCommand, ChangeRequestStatusResponse>, ChangeRequestStatusHandler>();
        services.AddScoped<IRequestHandler<AddRequestNoteCommand, AddRequestNoteResponse>, AddRequestNoteHandler>();
        services.AddScoped<IRequestHandler<SendRequestEmailCommand, SendRequestEmailResponse>, SendRequestEmailHandler>();
        services.AddScoped<IRequestHandler<AddRequestAttachmentCommand, AddRequestAttachmentResponse>, AddRequestAttachmentHandler>();

        services.AddScoped<IRequestHandler<SearchApprovalWorkflowsQuery, SearchApprovalWorkflowsResponse>, SearchApprovalWorkflowsHandler>();
        services.AddScoped<IRequestHandler<CreateApprovalWorkflowCommand, CreateApprovalWorkflowResponse>, CreateApprovalWorkflowHandler>();
        services.AddScoped<IRequestHandler<PublishApprovalWorkflowCommand, PublishApprovalWorkflowResponse>, PublishApprovalWorkflowHandler>();
        services.AddScoped<IRequestHandler<SubmitForApprovalCommand, SubmitForApprovalResponse>, SubmitForApprovalHandler>();
        services.AddScoped<IRequestHandler<SearchMyApprovalsQuery, SearchMyApprovalsResponse>, SearchMyApprovalsHandler>();
        services.AddScoped<IRequestHandler<GetRequestApprovalQuery, GetRequestApprovalResponse>, GetRequestApprovalHandler>();
        services.AddScoped<IRequestHandler<DecideApprovalCommand, DecideApprovalResponse>, DecideApprovalHandler>();
        services.AddScoped<IRequestHandler<CancelApprovalCommand, CancelApprovalResponse>, CancelApprovalHandler>();
    }
}
