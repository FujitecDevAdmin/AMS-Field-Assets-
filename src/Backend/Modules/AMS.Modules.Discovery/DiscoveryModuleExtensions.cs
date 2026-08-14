using AMS.Modules.Discovery.Features.IssueAgentKey;
using AMS.Modules.Discovery.Features.ReportInventory;
using AMS.Modules.Discovery.Features.ResolveDiscoveredDevice;
using AMS.Modules.Discovery.Features.RevokeAgentKey;
using AMS.Modules.Discovery.Features.SearchAgentKeys;
using AMS.Modules.Discovery.Features.SearchAssetHealth;
using AMS.Modules.Discovery.Features.SearchDiscoveredDevices;
using AMS.Modules.Discovery.Features.SearchInstalledSoftware;
using AMS.Modules.Discovery.Features.SetSoftwareCatalogEntry;
using AMS.Modules.Discovery.Persistence;
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

namespace AMS.Modules.Discovery;

/// <summary>
/// The module's single registration point. <c>Program.cs</c> is a list of
/// these calls and nothing else (docs/02 §9).
/// </summary>
/// <remarks>
/// Six tables fed by an agent running on every machine: what it is, how it is
/// doing, and what is installed on it.
/// </remarks>
public static class DiscoveryModuleExtensions
{
    public static IServiceCollection AddDiscoveryModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<DiscoveryDbContext>(DiscoveryDbContext.SchemaName);

        AddHandlers(services);

        services.AddValidatorsFromAssemblyContaining<ReportInventoryValidator>(
            ServiceLifetime.Scoped);

        // Every unique index in this schema, with the 409 it produces.
        // SqlErrorRegistrationTests reads the live schema and fails if one is
        // missing from this list (docs/03 §7).
        services.AddSingleton(new SqlErrorTranslator()
            .Register("UX_DiscoveredDevice_Machine", "DiscoveredDevice.AlreadyKnown",
                "That machine has already been discovered.")
            .Register("UX_AssetInstalledSoftware_Install", "InstalledSoftware.AlreadyRecorded",
                "That installation is already recorded against this asset.")
            .Register("UX_SoftwareCatalog_Name", "SoftwareCatalog.NameTaken",
                "That title is already in the catalogue."));

        return services;
    }

    /// <summary>Contributes this module's routes (01 §5).</summary>
    /// <remarks>
    /// The agent's endpoint is mapped OUTSIDE the authorised group. It carries
    /// no bearer token — an agent has no session — and requiring one would make
    /// the whole module unreachable by the software it exists to serve. Its own
    /// handler checks the API key.
    /// </remarks>
    public static IEndpointRouteBuilder MapDiscoveryModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/discovery")
            .RequireAuthorization()
            .AddEndpointFilter<ValidationEndpointFilter>();

        SearchAgentKeysEndpoint.Map(group);
        IssueAgentKeyEndpoint.Map(group);
        RevokeAgentKeyEndpoint.Map(group);

        SearchDiscoveredDevicesEndpoint.Map(group);
        ResolveDiscoveredDeviceEndpoint.Map(group);

        SearchAssetHealthEndpoint.Map(group);
        SearchInstalledSoftwareEndpoint.Map(group);
        SetSoftwareCatalogEntryEndpoint.Map(group);

        var agents = endpoints.MapGroup("/api/v1/discovery")
            .AddEndpointFilter<ValidationEndpointFilter>();

        ReportInventoryEndpoint.Map(agents);

        return endpoints;
    }

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<SearchAgentKeysQuery, SearchAgentKeysResponse>, SearchAgentKeysHandler>();
        services.AddScoped<IRequestHandler<IssueAgentKeyCommand, IssueAgentKeyResponse>, IssueAgentKeyHandler>();
        services.AddScoped<IRequestHandler<RevokeAgentKeyCommand, RevokeAgentKeyResponse>, RevokeAgentKeyHandler>();

        services.AddScoped<IRequestHandler<ReportInventoryCommand, ReportInventoryResponse>, ReportInventoryHandler>();

        services.AddScoped<IRequestHandler<SearchDiscoveredDevicesQuery, SearchDiscoveredDevicesResponse>, SearchDiscoveredDevicesHandler>();
        services.AddScoped<IRequestHandler<ResolveDiscoveredDeviceCommand, ResolveDiscoveredDeviceResponse>, ResolveDiscoveredDeviceHandler>();

        services.AddScoped<IRequestHandler<SearchAssetHealthQuery, SearchAssetHealthResponse>, SearchAssetHealthHandler>();
        services.AddScoped<IRequestHandler<SearchInstalledSoftwareQuery, SearchInstalledSoftwareResponse>, SearchInstalledSoftwareHandler>();
        services.AddScoped<IRequestHandler<SetSoftwareCatalogEntryCommand, SetSoftwareCatalogEntryResponse>, SetSoftwareCatalogEntryHandler>();
    }
}
