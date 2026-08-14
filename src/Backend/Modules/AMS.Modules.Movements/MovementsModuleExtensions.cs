using AMS.Modules.Movements.Features.DespatchAsset;
using AMS.Modules.Movements.Features.DespatchBatch;
using AMS.Modules.Movements.Features.GetGrnQueue;
using AMS.Modules.Movements.Features.ReceiveMovement;
using AMS.Modules.Movements.Features.SearchMovements;
using AMS.Modules.Movements.Persistence;
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

namespace AMS.Modules.Movements;

/// <summary>
/// The module's single registration point. <c>Program.cs</c> is a list of
/// these calls and nothing else (docs/02 §9).
/// </summary>
public static class MovementsModuleExtensions
{
    public static IServiceCollection AddMovementsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<MovementsDbContext>(MovementsDbContext.SchemaName);

        AddHandlers(services);

        services.AddValidatorsFromAssemblyContaining<DespatchAssetValidator>(ServiceLifetime.Scoped);

        // Every unique index in this schema, with the 409 it produces.
        // SqlErrorRegistrationTests reads the live schema and fails if one is
        // missing from this list (docs/03 §7).
        services.AddSingleton(new SqlErrorTranslator()
            .Register("UX_MovementBatch_Number", "MovementBatch.NumberTaken",
                "That consignment number is already in use."));

        return services;
    }

    /// <summary>Contributes this module's routes (01 §5).</summary>
    public static IEndpointRouteBuilder MapMovementsModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/movements")
            .RequireAuthorization()
            .AddEndpointFilter<ValidationEndpointFilter>();

        SearchMovementsEndpoint.Map(group);
        DespatchAssetEndpoint.Map(group);
        DespatchBatchEndpoint.Map(group);
        GetGrnQueueEndpoint.Map(group);
        ReceiveMovementEndpoint.Map(group);

        return endpoints;
    }

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<SearchMovementsQuery, SearchMovementsResponse>, SearchMovementsHandler>();
        services.AddScoped<IRequestHandler<DespatchAssetCommand, DespatchAssetResponse>, DespatchAssetHandler>();
        services.AddScoped<IRequestHandler<DespatchBatchCommand, DespatchBatchResponse>, DespatchBatchHandler>();
        services.AddScoped<IRequestHandler<GetGrnQueueQuery, GetGrnQueueResponse>, GetGrnQueueHandler>();
        services.AddScoped<IRequestHandler<ReceiveMovementCommand, ReceiveMovementResponse>, ReceiveMovementHandler>();
    }
}
