using AMS.Modules.Contracts.Features.AddContractDocument;
using AMS.Modules.Contracts.Features.CreateContract;
using AMS.Modules.Contracts.Features.GetContract;
using AMS.Modules.Contracts.Features.RenewContract;
using AMS.Modules.Contracts.Features.SearchContracts;
using AMS.Modules.Contracts.Features.SetContractAssets;
using AMS.Modules.Contracts.Features.SetReminderWindows;
using AMS.Modules.Contracts.Features.UpdateContract;
using AMS.Modules.Contracts.Persistence;
using AMS.Modules.Contracts.Reminders;
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

namespace AMS.Modules.Contracts;

/// <summary>
/// The module's single registration point. <c>Program.cs</c> is a list of
/// these calls and nothing else (docs/02 §9).
/// </summary>
/// <remarks>
/// AMCs, warranties, leases, licences, service agreements and insurance. R3
/// widened this past IT, because a lease on a building has an expiry date
/// somebody must be reminded about exactly like an AMC on a laptop.
/// </remarks>
public static class ContractsModuleExtensions
{
    public static IServiceCollection AddContractsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<ContractsDbContext>(ContractsDbContext.SchemaName);

        services.AddScoped<LicenceKeyProtector>();
        services.AddScoped<ContractReminderWorker>();

        AddHandlers(services);

        services.AddValidatorsFromAssemblyContaining<CreateContractValidator>(
            ServiceLifetime.Scoped);

        // Every unique index in this schema, with the 409 it produces.
        // SqlErrorRegistrationTests reads the live schema and fails if one is
        // missing from this list (docs/03 §7).
        services.AddSingleton(new SqlErrorTranslator()
            .Register("UX_Contract_Number", "Contract.NumberTaken",
                "A contract with that number already exists.")
            .Register("UX_ContractAsset_NoDuplicates", "Contract.AssetAlreadyCovered",
                "That asset is already covered by this contract.")
            // R2-2 and R2-3: the key includes the expiry it was measured
            // against, and excludes failed attempts so one can be retried.
            .Register("UX_ContractReminderLog_OncePerThreshold", "ContractReminder.AlreadySent",
                "That reminder has already gone out for this expiry date.")
            .Register("UX_ContractReminderSetting_Default", "ContractReminder.DefaultExists",
                "The organisation already has a reminder at that many days.")
            .Register("UX_ContractReminderSetting_PerContract", "ContractReminder.WindowExists",
                "This contract already has a reminder at that many days."));

        return services;
    }

    /// <summary>Contributes this module's routes (01 §5).</summary>
    public static IEndpointRouteBuilder MapContractsModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/contracts")
            .RequireAuthorization()
            .AddEndpointFilter<ValidationEndpointFilter>();

        // The reminder settings route is registered FIRST, so /reminder-windows
        // is matched before /{id:int} has a chance to try.
        SetReminderWindowsEndpoint.Map(group);

        SearchContractsEndpoint.Map(group);
        CreateContractEndpoint.Map(group);
        GetContractEndpoint.Map(group);
        UpdateContractEndpoint.Map(group);
        RenewContractEndpoint.Map(group);
        SetContractAssetsEndpoint.Map(group);
        AddContractDocumentEndpoint.Map(group);

        return endpoints;
    }

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<SearchContractsQuery, SearchContractsResponse>, SearchContractsHandler>();
        services.AddScoped<IRequestHandler<GetContractQuery, GetContractResponse>, GetContractHandler>();
        services.AddScoped<IRequestHandler<CreateContractCommand, CreateContractResponse>, CreateContractHandler>();
        services.AddScoped<IRequestHandler<UpdateContractCommand, UpdateContractResponse>, UpdateContractHandler>();
        services.AddScoped<IRequestHandler<RenewContractCommand, RenewContractResponse>, RenewContractHandler>();
        services.AddScoped<IRequestHandler<SetContractAssetsCommand, SetContractAssetsResponse>, SetContractAssetsHandler>();
        services.AddScoped<IRequestHandler<AddContractDocumentCommand, AddContractDocumentResponse>, AddContractDocumentHandler>();
        services.AddScoped<IRequestHandler<SetReminderWindowsCommand, SetReminderWindowsResponse>, SetReminderWindowsHandler>();
    }
}
