using AMS.Modules.Allocations.Features.AllocateAsset;
using AMS.Modules.Allocations.Features.ApproveAcknowledgement;
using AMS.Modules.Allocations.Features.CreateCustomerSite;
using AMS.Modules.Allocations.Features.DecideAllocationRequest;
using AMS.Modules.Allocations.Features.GetMyAssets;
using AMS.Modules.Allocations.Features.MapAssetToSite;
using AMS.Modules.Allocations.Features.ReceiveReturn;
using AMS.Modules.Allocations.Features.RecordHandover;
using AMS.Modules.Allocations.Features.RemoveAssetFromSite;
using AMS.Modules.Allocations.Features.RequestAllocation;
using AMS.Modules.Allocations.Features.RequestReturn;
using AMS.Modules.Allocations.Features.ReverseReturn;
using AMS.Modules.Allocations.Features.SearchAllocationRequests;
using AMS.Modules.Allocations.Features.SearchAllocations;
using AMS.Modules.Allocations.Features.SearchCustomerSites;
using AMS.Modules.Allocations.Features.SearchHandovers;
using AMS.Modules.Allocations.Features.SignAcknowledgement;
using AMS.Modules.Allocations.Features.UpdateCustomerSite;
using AMS.Modules.Allocations.Persistence;
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

namespace AMS.Modules.Allocations;

/// <summary>
/// The module's single registration point. <c>Program.cs</c> is a list of
/// these calls and nothing else (docs/02 §9).
/// </summary>
public static class AllocationsModuleExtensions
{
    public static IServiceCollection AddAllocationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<AllocationsDbContext>(AllocationsDbContext.SchemaName);

        AddHandlers(services);

        services.AddValidatorsFromAssemblyContaining<AllocateAssetValidator>(ServiceLifetime.Scoped);

        // Every unique index in this schema, with the 409 it produces.
        // SqlErrorRegistrationTests reads the live schema and fails if one is
        // missing from this list (docs/03 §7).
        services.AddSingleton(new SqlErrorTranslator()
            // The one this module is built around: one live allocation per
            // asset, filtered on ReturnedOnUtc IS NULL. Two people allocating
            // the same asset at once collide here rather than both succeeding.
            .Register("UX_AssetAllocation_OneActivePerAsset", "Allocation.AssetAlreadyIssued",
                "That asset is already issued to somebody.")
            .Register("UX_AssetAcknowledgement_Allocation", "Acknowledgement.AlreadyExists",
                "That allocation already has an acknowledgement.")
            .Register("UX_AssetHandover_OneOpenPerAsset", "Handover.AlreadyInStore",
                "That asset is already sitting in a branch store.")
            .Register("UX_AssetHandover_OnePerAllocation", "Handover.AlreadyRecorded",
                "That allocation has already been handed over.")
            .Register("UX_AssetSiteMapping_OneActivePerAsset", "SiteMapping.AlreadyOnSite",
                "That asset is already at a customer site."));

        return services;
    }

    /// <summary>Contributes this module's routes (01 §5).</summary>
    public static IEndpointRouteBuilder MapAllocationsModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/allocations")
            .RequireAuthorization()
            // Shape checks run before the endpoint, so no endpoint has to
            // remember to ask (02 §6).
            .AddEndpointFilter<ValidationEndpointFilter>();

        // The allocation itself.
        SearchAllocationsEndpoint.Map(group);
        AllocateAssetEndpoint.Map(group);
        ReceiveReturnEndpoint.Map(group);
        ReverseReturnEndpoint.Map(group);

        // The approval queue in front of it.
        SearchAllocationRequestsEndpoint.Map(group);
        RequestAllocationEndpoint.Map(group);
        DecideAllocationRequestEndpoint.Map(group);

        // What the employee sees and does.
        GetMyAssetsEndpoint.Map(group);
        RequestReturnEndpoint.Map(group);
        SignAcknowledgementEndpoint.Map(group);
        ApproveAcknowledgementEndpoint.Map(group);

        // The branch store.
        SearchHandoversEndpoint.Map(group);
        RecordHandoverEndpoint.Map(group);

        // Customer sites.
        SearchCustomerSitesEndpoint.Map(group);
        CreateCustomerSiteEndpoint.Map(group);
        UpdateCustomerSiteEndpoint.Map(group);
        MapAssetToSiteEndpoint.Map(group);
        RemoveAssetFromSiteEndpoint.Map(group);

        return endpoints;
    }

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<SearchAllocationsQuery, SearchAllocationsResponse>, SearchAllocationsHandler>();
        services.AddScoped<IRequestHandler<AllocateAssetCommand, AllocateAssetResponse>, AllocateAssetHandler>();
        services.AddScoped<IRequestHandler<ReceiveReturnCommand, ReceiveReturnResponse>, ReceiveReturnHandler>();
        services.AddScoped<IRequestHandler<ReverseReturnCommand, ReverseReturnResponse>, ReverseReturnHandler>();

        services.AddScoped<IRequestHandler<SearchAllocationRequestsQuery, SearchAllocationRequestsResponse>, SearchAllocationRequestsHandler>();
        services.AddScoped<IRequestHandler<RequestAllocationCommand, RequestAllocationResponse>, RequestAllocationHandler>();
        services.AddScoped<IRequestHandler<DecideAllocationRequestCommand, DecideAllocationRequestResponse>, DecideAllocationRequestHandler>();

        services.AddScoped<IRequestHandler<GetMyAssetsQuery, GetMyAssetsResponse>, GetMyAssetsHandler>();
        services.AddScoped<IRequestHandler<RequestReturnCommand, RequestReturnResponse>, RequestReturnHandler>();
        services.AddScoped<IRequestHandler<SignAcknowledgementCommand, SignAcknowledgementResponse>, SignAcknowledgementHandler>();
        services.AddScoped<IRequestHandler<ApproveAcknowledgementCommand, ApproveAcknowledgementResponse>, ApproveAcknowledgementHandler>();

        services.AddScoped<IRequestHandler<SearchHandoversQuery, SearchHandoversResponse>, SearchHandoversHandler>();
        services.AddScoped<IRequestHandler<RecordHandoverCommand, RecordHandoverResponse>, RecordHandoverHandler>();

        services.AddScoped<IRequestHandler<SearchCustomerSitesQuery, SearchCustomerSitesResponse>, SearchCustomerSitesHandler>();
        services.AddScoped<IRequestHandler<CreateCustomerSiteCommand, CreateCustomerSiteResponse>, CreateCustomerSiteHandler>();
        services.AddScoped<IRequestHandler<UpdateCustomerSiteCommand, UpdateCustomerSiteResponse>, UpdateCustomerSiteHandler>();
        services.AddScoped<IRequestHandler<MapAssetToSiteCommand, MapAssetToSiteResponse>, MapAssetToSiteHandler>();
        services.AddScoped<IRequestHandler<RemoveAssetFromSiteCommand, RemoveAssetFromSiteResponse>, RemoveAssetFromSiteHandler>();
    }
}
