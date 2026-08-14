using AMS.Modules.Transfers.Features.CancelTransfer;
using AMS.Modules.Transfers.Features.CompleteTransfer;
using AMS.Modules.Transfers.Features.DecideTransfer;
using AMS.Modules.Transfers.Features.RaiseTransfer;
using AMS.Modules.Transfers.Features.SearchTransferRequests;
using AMS.Modules.Transfers.Persistence;
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

namespace AMS.Modules.Transfers;

/// <summary>
/// The module's single registration point. <c>Program.cs</c> is a list of
/// these calls and nothing else (docs/02 §9).
/// </summary>
public static class TransfersModuleExtensions
{
    public static IServiceCollection AddTransfersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<TransfersDbContext>(TransfersDbContext.SchemaName);

        AddHandlers(services);

        services.AddValidatorsFromAssemblyContaining<RaiseTransferValidator>(ServiceLifetime.Scoped);

        // This schema has no unique indexes — one table, two ordinary indexes —
        // so the translator is empty. Registered anyway because the handlers
        // inject it, and an empty one is honest: it says nothing here produces
        // a 409 from the database, rather than leaving the question open.
        services.AddSingleton(new SqlErrorTranslator());

        return services;
    }

    /// <summary>Contributes this module's routes (01 §5).</summary>
    public static IEndpointRouteBuilder MapTransfersModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/transfers")
            .RequireAuthorization()
            .AddEndpointFilter<ValidationEndpointFilter>();

        SearchTransferRequestsEndpoint.Map(group);
        RaiseTransferEndpoint.Map(group);
        DecideTransferEndpoint.Map(group);
        CompleteTransferEndpoint.Map(group);
        CancelTransferEndpoint.Map(group);

        return endpoints;
    }

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<SearchTransferRequestsQuery, SearchTransferRequestsResponse>, SearchTransferRequestsHandler>();
        services.AddScoped<IRequestHandler<RaiseTransferCommand, RaiseTransferResponse>, RaiseTransferHandler>();
        services.AddScoped<IRequestHandler<DecideTransferCommand, DecideTransferResponse>, DecideTransferHandler>();
        services.AddScoped<IRequestHandler<CompleteTransferCommand, CompleteTransferResponse>, CompleteTransferHandler>();
        services.AddScoped<IRequestHandler<CancelTransferCommand, CancelTransferResponse>, CancelTransferHandler>();
    }
}
